using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using MjrChess.Engine;
using MjrChess.Engine.Models;

namespace IngestionFunctions.Services
{
    /// <summary>
    /// Service for searching LiChess games.
    /// </summary>
    public class LiChessService : IChessService
    {
        private const string UrlFormatString = "https://lichess.org/api/games/user/{0}?max={1}{2}";

        private ChessEngine Engine { get; }

        private HttpClient HttpClient { get; }

        private ILogger<LiChessService> Logger { get; }

        public LiChessService(HttpClient httpClient, ILogger<LiChessService> logger, ChessEngine engine)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Find completed chess games from lichess played by the specified player (beginning with the newest games).
        /// </summary>
        /// <param name="playerName">The player to get games for.</param>
        /// <param name="since">Unix millisecond timestamp for the oldest game to retrieve. If not specified, defaults to player's account creation date.</param>
        /// <param name="max">Maximum games to retrieve (default 100).</param>
        /// <returns>An async enumerable of the newest games for the specified player.</returns>
        public async IAsyncEnumerable<ChessGame> GetPlayerGamesAsync(string playerName, DateTimeOffset? since, int max = 100)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException("Player name must not be null or empty", nameof(playerName));
            }

            var pgnLines = new List<string>();
            var sinceClause = since.HasValue ? $"&since={since.Value.ToUnixTimeMilliseconds()}" : string.Empty;

            // Query LiChess's /games/user endpoint
            var uri = new Uri(string.Format(CultureInfo.InvariantCulture, UrlFormatString, playerName, max, sinceClause));
            using var response = await HttpClient.GetStreamAsync(uri);

            if (response == null)
            {
                Logger.LogError("HTTP request to {URI} failed", uri);
            }
            else
            {
                using var responseReader = new StreamReader(response);

                // Read the response one line at a time
                var line = await responseReader.ReadLineAsync();

                while (line != null)
                {
                    pgnLines.Add(line);
                    if (line.StartsWith("1."))
                    {
                        // If the line contains game moves, then return the game and reset the PGN line collection
                        Engine.LoadPGN(string.Join('\n', pgnLines));
                        pgnLines = new List<string>();
                        Logger.LogInformation("Read game {GamePath}", Engine.Game.Site);
                        yield return Engine.Game;
                    }

                    // Read the next line
                    line = await responseReader.ReadLineAsync();
                }
            }
        }
    }
}

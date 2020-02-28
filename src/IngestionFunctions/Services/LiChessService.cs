using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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

            var response = await HttpClient.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("HTTP request to {URI} failed: {StatusCode}: {ErrorMessage}", uri, response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                using var responseBody = await response.Content.ReadAsStreamAsync();

                if (response == null)
                {
                    Logger.LogError("HTTP request to {URI} failed", uri);
                }
                else
                {
                    using var responseReader = new StreamReader(responseBody);

                    // Read the response one line at a time
                    var line = await responseReader.ReadLineAsync();
                    const string utcDatePrefix = "[UTCDate \"";
                    const string utcTimePrefix = "[UTCTime \"";
                    string? utcDate = null;
                    string? utcTime = null;

                    while (line != null)
                    {
                        pgnLines.Add(line);
                        if (line.StartsWith(utcDatePrefix))
                        {
                            utcDate = line.Substring(utcDatePrefix.Length, 10);
                        }
                        else if (line.StartsWith(utcTimePrefix))
                        {
                            utcTime = line.Substring(utcTimePrefix.Length, 8);
                        }
                        else if (line.StartsWith("1."))
                        {
                            // If the line contains game moves, then return the game and reset the PGN line collection
                            Engine.LoadPGN(string.Join('\n', pgnLines));

                            // Set the game time more precisely
                            if (utcDate != null && utcTime != null)
                            {
                                Engine.Game.StartDate = DateTimeOffset.ParseExact($"{utcDate}|{utcTime}", "yyyy.MM.dd|HH:mm:ss", CultureInfo.InvariantCulture);
                            }

                            // Reset variables
                            pgnLines = new List<string>();
                            utcDate = null;
                            utcTime = null;

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
}

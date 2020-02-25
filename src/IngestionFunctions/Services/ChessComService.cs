using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MjrChess.Engine;
using MjrChess.Engine.Models;

namespace IngestionFunctions.Services
{
    public class ChessComService : IChessService
    {
        private ChessEngine Engine { get; }

        private HttpClient HttpClient { get; }

        private ILogger<ChessComService> Logger { get; }

        public ChessComService(HttpClient httpClient, ILogger<ChessComService> logger, ChessEngine engine)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public async IAsyncEnumerable<ChessGame> GetPlayerGamesAsync(string playerName, DateTimeOffset? since, int max)
        {
            // TODO
            Logger.LogWarning("Chess.com game processing not yet implemented.");

            await Task.Yield();
            foreach (var game in Enumerable.Empty<ChessGame>())
            {
                yield return game;
            }
        }
    }
}

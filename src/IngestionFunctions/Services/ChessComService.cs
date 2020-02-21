using System;
using System.Collections.Generic;
using MjrChess.Engine.Models;

namespace IngestionFunctions.Services
{
    public class ChessComService : IChessService
    {
        public IAsyncEnumerable<ChessGame> GetPlayerGamesAsync(string playerName, DateTimeOffset? since, int max)
        {
            throw new NotImplementedException();
        }
    }
}

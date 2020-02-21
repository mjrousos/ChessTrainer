using System;
using System.Collections.Generic;
using MjrChess.Engine.Models;
using MjrChess.Trainer.Models;

namespace IngestionFunctions.Services
{
    public interface IChessService
    {
        IAsyncEnumerable<ChessGame> GetPlayerGamesAsync(string playerName, DateTimeOffset? since, int max);
    }

    public delegate IChessService ChessServiceResolver(ChessSites site);
}

﻿using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public interface IPlayerService
    {
        Task<Player> GetOrAddPlayerAsync(string name, ChessSites site);

        Task DeletePlayerAsync(int playerId);
    }
}

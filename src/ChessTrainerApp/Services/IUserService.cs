using System.Collections.Generic;
using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public interface IUserService
    {
        Task<IEnumerable<Player>> GetPreferredPlayersAsync(string userId);

        Task<bool> AddPreferredPlayerAsync(string userId, string playerName, ChessSites playerSite);

        Task<bool> AddPreferredPlayerAsync(string userId, Player player);

        Task<bool> RemovePreferredPlayerAsync(string userId, int playerId);

        Task<IEnumerable<PuzzleHistory>> GetPuzzleHistoryAsync(string userId);

        Task RecordPuzzleHistoryAsync(PuzzleHistory puzzleHistory);
    }
}

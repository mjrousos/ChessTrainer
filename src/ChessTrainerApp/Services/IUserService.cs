using System.Collections.Generic;
using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public interface IUserService
    {
        Task<IEnumerable<PuzzleHistory>> GetPuzzleHistoryAsync(string userId);

        Task RecordPuzzleHistoryAsync(PuzzleHistory puzzleHistory);
    }
}

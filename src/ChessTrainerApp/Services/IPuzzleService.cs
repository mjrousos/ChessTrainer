using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public interface IPuzzleService
    {
        Task<TacticsPuzzle?> GetRandomPuzzleAsync(string? userId);

        Task<TacticsPuzzle?> GetPuzzleAsync(int puzzleId);
    }
}

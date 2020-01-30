using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public interface IPuzzleService
    {
        Task<TacticsPuzzle> GetPuzzleAsync();
    }
}

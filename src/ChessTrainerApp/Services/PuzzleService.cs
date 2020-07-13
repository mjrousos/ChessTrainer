using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public class PuzzleService : IPuzzleService
    {
        private static Random NumGen { get; } = new Random();

        private IRepository<TacticsPuzzle> PuzzleRepository { get; }

        private ILogger<PuzzleService> Logger { get; }

        public PuzzleService(IRepository<TacticsPuzzle> puzzleRepository,
                             ILogger<PuzzleService> logger)
        {
            PuzzleRepository = puzzleRepository ?? throw new ArgumentNullException(nameof(puzzleRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TacticsPuzzle?> GetPuzzleAsync(int puzzleId)
        {
            var puzzle = await PuzzleRepository.GetAsync(puzzleId);
            if (puzzle == null)
            {
                Logger.LogInformation("Puzzle {PuzzleId} not found", puzzleId);
            }
            else
            {
                Logger.LogInformation("Retrieved puzzle {PuzzleId}", puzzleId);
            }

            return puzzle;
        }

        public async Task<TacticsPuzzle?> GetRandomPuzzleAsync(string? userId)
        {
            var puzzlesQuery = PuzzleRepository.Query();
            var puzzleCount = await puzzlesQuery.CountAsync();
            var skipCount = NumGen.Next(puzzleCount);
            var puzzle = await puzzlesQuery.Skip(skipCount).FirstOrDefaultAsync();
            if (puzzle == null)
            {
                Logger.LogError("No puzzles found");
            }
            else
            {
                // Automapper doesn't map the puzzle's history's puzzles properly, so fix that up here.
                foreach (var history in puzzle.History)
                {
                    history.Puzzle = puzzle;
                }

                Logger.LogInformation("Retrieved puzzle {PuzzleId} for user {UserId} (index {SkipCount} of {PuzzleCount} puzzles)",
                    puzzle.Id,
                    userId ?? "Anonymous",
                    skipCount,
                    puzzleCount);
            }

            return puzzle;
        }
    }
}

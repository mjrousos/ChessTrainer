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

        private IRepository<UserSettings> UserSettingsRepository { get; }

        private ILogger<PuzzleService> Logger { get; }

        public PuzzleService(IRepository<TacticsPuzzle> puzzleRepository,
                             IRepository<UserSettings> userSettingsRepository,
                             ILogger<PuzzleService> logger)
        {
            PuzzleRepository = puzzleRepository ?? throw new ArgumentNullException(nameof(puzzleRepository));
            UserSettingsRepository = userSettingsRepository ?? throw new ArgumentNullException(nameof(userSettingsRepository));
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
            var puzzlesQuery = await GetPuzzlesAsync(userId);
            var puzzleCount = await puzzlesQuery.CountAsync();
            if (puzzleCount == 0)
            {
                Logger.LogInformation("None of user {UserId}'s preferred players have games in the database", userId ?? "Anonymous");
                puzzlesQuery = PuzzleRepository.Query(null);
                puzzleCount = await puzzlesQuery.CountAsync();
            }

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

        private async Task<IQueryable<TacticsPuzzle>> GetPuzzlesAsync(string? userId)
        {
            var puzzles = PuzzleRepository.Query(null);

            if (userId != null)
            {
                var userSettings = await UserSettingsRepository.Query(s => string.Equals(userId, s.UserId))
                    .SingleOrDefaultAsync();

                if (userSettings?.PreferredPlayers != null && userSettings.PreferredPlayers.Count > 0)
                {
                    var preferredIds = userSettings.PreferredPlayers.Select(p => p.Id);
                    Logger.LogInformation("Retrieving puzzles for {UserId} with {PreferredPlayerCount} preferred players", userId, preferredIds.Count());
                    puzzles = PuzzleRepository.Query(p => preferredIds.Contains(p.AssociatedPlayerId));
                }
                else
                {
                    Logger.LogInformation("Retrieving puzzles for {UserId} with {PreferredPlayerCount} preferred players", userId, 0);
                }
            }
            else
            {
                Logger.LogInformation("Retrieving puzzles for anonymous user");
            }

            return puzzles;
        }
    }
}

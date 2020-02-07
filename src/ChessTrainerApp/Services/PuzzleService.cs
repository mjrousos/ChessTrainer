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

        public CurrentUserService UserService { get; }

        private ILogger<PuzzleService> Logger { get; }

        public PuzzleService(IRepository<TacticsPuzzle> puzzleRepository,
                             IRepository<UserSettings> userSettingsRepository,
                             CurrentUserService userService,
                             ILogger<PuzzleService> logger)
        {
            PuzzleRepository = puzzleRepository ?? throw new ArgumentNullException(nameof(puzzleRepository));
            UserSettingsRepository = userSettingsRepository ?? throw new ArgumentNullException(nameof(userSettingsRepository));
            UserService = userService ?? throw new ArgumentNullException(nameof(userService));
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

        public async Task<TacticsPuzzle?> GetRandomPuzzleAsync()
        {
            var puzzlesQuery = await GetPuzzlesForCurrentUserAsync();
            var puzzleCount = await puzzlesQuery.CountAsync();
            if (puzzleCount == 0)
            {
                Logger.LogInformation("None of user {UserId}'s preferred players have games in the database", UserService.CurrentUserId ?? "Anonymous");
                puzzlesQuery = await PuzzleRepository.GetAllAsync();
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
                Logger.LogInformation("Retrieved puzzle {PuzzleId} for user {UserId} (index {SkipCount} of {PuzzleCount} puzzles)",
                    puzzle.Id,
                    UserService.CurrentUserId ?? "Anonymous",
                    skipCount,
                    puzzleCount);
            }

            return puzzle;
        }

        private async Task<IQueryable<TacticsPuzzle>> GetPuzzlesForCurrentUserAsync()
        {
            var puzzles = await PuzzleRepository.GetAllAsync();

            if (UserService.CurrentUserId != null)
            {
                var userSettings = await (await UserSettingsRepository.GetAllAsync())
                    .Where(s => string.Equals(UserService.CurrentUserId, s.UserId))
                    .SingleOrDefaultAsync();

                if (userSettings?.PreferredPlayers != null && userSettings.PreferredPlayers.Count > 0)
                {
                    var preferredIds = userSettings.PreferredPlayers.Select(p => p.PlayerId);
                    Logger.LogInformation("Retrieving puzzles for {UserId} with {PreferredPlayerCount} preferred players", UserService.CurrentUserId, preferredIds.Count());
                    puzzles = puzzles.Where(p =>
                        (p.WhitePlayer != null && preferredIds.Contains(p.WhitePlayer.Id)) ||
                        (p.BlackPlayer != null && preferredIds.Contains(p.BlackPlayer.Id)));
                }
                else
                {
                    Logger.LogInformation("Retrieving puzzles for {UserId} with {PreferredPlayerCount} preferred players", UserService.CurrentUserId, 0);
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

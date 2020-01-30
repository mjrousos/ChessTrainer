using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public class PuzzleService : IPuzzleService
    {
        private static Random NumGen { get; } = new Random();

        private ClaimsPrincipal? CurrentUser { get; }

        private string? CurrentUserId => CurrentUser?.GetUserId();

        private IRepository<TacticsPuzzle> PuzzleRepository { get; }

        private IRepository<UserSettings> UserSettingsRepository { get; }

        private ILogger<PuzzleService> Logger { get; }

        public PuzzleService(IRepository<TacticsPuzzle> puzzleRepository,
                             IRepository<UserSettings> userSettingsRepository,
                             IHttpContextAccessor httpContextAccessor,
                             ILogger<PuzzleService> logger)
        {
            CurrentUser = httpContextAccessor?.HttpContext.User; // TODO : Move to its own simple user service
            PuzzleRepository = puzzleRepository ?? throw new ArgumentNullException(nameof(puzzleRepository));
            UserSettingsRepository = userSettingsRepository ?? throw new ArgumentNullException(nameof(userSettingsRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TacticsPuzzle> GetPuzzleAsync()
        {
            var puzzlesQuery = await GetPuzzlesForCurrentUserAsync();
            var puzzleCount = await puzzlesQuery.CountAsync();
            var skipCount = NumGen.Next(puzzleCount);
            var puzzle = puzzlesQuery.Skip(skipCount).First();
            Logger.LogInformation("Retrieved puzzle {PuzzleId} for user {UserId} (index {SkipCount} of {PuzzleCount} puzzles)",
                puzzle.Id,
                CurrentUserId ?? "Anonymous",
                skipCount,
                puzzleCount);

            return puzzle;
        }

        private async Task<IQueryable<TacticsPuzzle>> GetPuzzlesForCurrentUserAsync()
        {
            var puzzles = await PuzzleRepository.GetAllAsync();

            if (CurrentUserId != null)
            {
                var userSettings = await (await UserSettingsRepository.GetAllAsync())
                    .Where(s => CurrentUserId.Equals(s.UserId, StringComparison.Ordinal))
                    .SingleOrDefaultAsync();

                if (userSettings.PreferredPlayers != null && userSettings.PreferredPlayers.Count > 0)
                {
                    var preferredIds = userSettings.PreferredPlayers.Select(p => p.Id);
                    Logger.LogInformation("Retrieving puzzles for {UserId} with {PreferredPlayerCount} preferred players", CurrentUserId, preferredIds.Count());
                    puzzles = puzzles.Where(p =>
                        (p.WhitePlayer != null && preferredIds.Contains(p.WhitePlayer.Id)) ||
                        (p.BlackPlayer != null && preferredIds.Contains(p.BlackPlayer.Id)));
                }
                else
                {
                    Logger.LogInformation("Retrieving puzzles for {UserId} with {PreferredPlayerCount} preferred players", CurrentUserId, 0);
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public class UserService : IUserService
    {
        public IRepository<UserSettings> UserSettingsRepository { get; }

        public IRepository<PuzzleHistory> PuzzleHistoryRepository { get; }

        public ILogger<UserService> Logger { get; }

        public UserService(IRepository<UserSettings> userSettingsRepository, IRepository<PuzzleHistory> puzzleHistoryRepository, ILogger<UserService> logger)
        {
            UserSettingsRepository = userSettingsRepository ?? throw new ArgumentNullException(nameof(userSettingsRepository));
            PuzzleHistoryRepository = puzzleHistoryRepository ?? throw new ArgumentNullException(nameof(puzzleHistoryRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<PuzzleHistory>> GetPuzzleHistoryAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogError("UserId must not be null or empty when calling GetPuzzleHistoryAsync");
                throw new ArgumentException(nameof(userId));
            }

            var history = await PuzzleHistoryRepository.Query(h => string.Equals(userId, h.UserId)).ToArrayAsync();
            Logger.LogInformation("Found {PuzzleCount} puzzle attempts for user {UserId}", history.Length, userId);

            return history;
        }

        public async Task RecordPuzzleHistoryAsync(PuzzleHistory puzzleHistory)
        {
            if (puzzleHistory is null)
            {
                throw new ArgumentNullException(nameof(puzzleHistory));
            }

            await PuzzleHistoryRepository.AddAsync(puzzleHistory);
            Logger.LogInformation("Recorded that user {UserId} {Result} puzzle {PuzzleId}", puzzleHistory.UserId, puzzleHistory.Solved ? "solved" : "failed", puzzleHistory.Puzzle.Id);
        }
    }
}

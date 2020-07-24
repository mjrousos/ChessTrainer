﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    /// <summary>
    /// Service for retrieving and updating users' attempts to solve tactics puzzles.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        private IRepository<PuzzleHistory> PuzzleHistoryRepository { get; }

        private ILogger<HistoryService> Logger { get; }

        public HistoryService(IRepository<PuzzleHistory> puzzleHistoryRepository, ILogger<HistoryService> logger)
        {
            PuzzleHistoryRepository = puzzleHistoryRepository ?? throw new ArgumentNullException(nameof(puzzleHistoryRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets puzzles a user has attempted to solve.
        /// </summary>
        /// <param name="userId">The user to retrieve puzzle histories for.</param>
        /// <returns>An enumerable of puzzle histories for the given user, recording puzzles they have solved or failed.</returns>
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

        /// <summary>
        /// Record an attempt to solve a puzzle.
        /// </summary>
        /// <param name="puzzleHistory">The puzzle attempt, including the user's ID, the puzzle, and whether it was solved successfully or not.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

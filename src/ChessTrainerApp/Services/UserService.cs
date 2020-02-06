using System;
using System.Collections.Generic;
using System.Linq;
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

        public IRepository<Player> PlayersRepository { get; }

        public IRepository<PuzzleHistory> PuzzleHistoryRepository { get; }

        public ILogger<UserService> Logger { get; }

        public UserService(IRepository<UserSettings> userSettingsRepository, IRepository<Player> playersRepository,
                           IRepository<PuzzleHistory> puzzleHistoryRepository, ILogger<UserService> logger)
        {
            UserSettingsRepository = userSettingsRepository ?? throw new ArgumentNullException(nameof(userSettingsRepository));
            PlayersRepository = playersRepository ?? throw new ArgumentNullException(nameof(playersRepository));
            PuzzleHistoryRepository = puzzleHistoryRepository ?? throw new ArgumentNullException(nameof(puzzleHistoryRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> AddPreferredPlayerAsync(string userId, string playerName, ChessSites playerSite)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Logger.LogError("Player name must not be null or empty");
                throw new ArgumentException(nameof(playerName));
            }

            var player = await (await PlayersRepository.GetAllAsync()).FirstOrDefaultAsync(p => playerName.ToLower().Equals(p.Name.ToLower()) && playerSite == p.Site);
            if (player is null)
            {
                player = await PlayersRepository.AddAsync(new Player(playerName) { Site = playerSite });
                Logger.LogInformation("Added new player {PlayerId}", player.Id);
            }

            return await AddPreferredPlayerAsync(userId, player);
        }

        public async Task<bool> AddPreferredPlayerAsync(string userId, Player player)
        {
            if (player is null)
            {
                Logger.LogError("Player must not be null when calling AddPreferredPlayerAsync");
                throw new ArgumentNullException(nameof(player));
            }

            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogError("UserId must not be empty or null when calling AddPreferredPlayerAsync");
                throw new ArgumentNullException(nameof(userId));
            }

            var settings = await GetUserSettingsAsync(userId);
            if (settings is null)
            {
                settings = await UserSettingsRepository.AddAsync(new UserSettings
                {
                    UserId = userId
                });
                Logger.LogInformation("Created user settings for new user {UserId}", userId);
            }

            if (settings.PreferredPlayers.Select(x => x.Player.Id).Contains(player.Id))
            {
                Logger.LogInformation("Not adding player {PlayerId} to user {UserId} preferences, because player is already preferred by that user", player.Id, userId);
                return false;
            }

            settings.PreferredPlayers.Add(new UserSettingsXPlayer { UserSettings = settings, Player = player });
            await UserSettingsRepository.UpdateAsync(settings);
            Logger.LogInformation("Player {PlayerId} added to user {UserId} preferences", player.Id, userId);

            return true;
        }

        public async Task<IEnumerable<Player>> GetPreferredPlayersAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogError("UserId must not be null or empty when calling GetPreferredPlayersAsync");
                throw new ArgumentNullException(nameof(userId));
            }

            var settings = await GetUserSettingsAsync(userId);

            if (settings is null)
            {
                Logger.LogInformation("No preferred players found for user {UserId}", userId);
                return Enumerable.Empty<Player>();
            }

            return settings.PreferredPlayers.Select(p => p.Player);
        }

        public async Task<bool> RemovePreferredPlayerAsync(string userId, int playerId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogError("UserId must not be null or empty when calling GetPreferredPlayersAsync");
                throw new ArgumentNullException(nameof(userId));
            }

            var settings = await GetUserSettingsAsync(userId);
            if (settings is null)
            {
                Logger.LogInformation("No preferred players found for user {UserId}", userId);
                return false;
            }
            else
            {
                var playerSetting = settings.PreferredPlayers.FirstOrDefault(x => x.PlayerId == playerId);
                if (playerSetting == null)
                {
                    Logger.LogInformation("Could not remove preferred player {PlayerId} from user {UserId} because the user does not prefer that player", playerId, userId);
                    return false;
                }
                else
                {
                    settings.PreferredPlayers.Remove(playerSetting);
                    await UserSettingsRepository.UpdateAsync(settings);
                    await CleanUpPlayerAsync(playerSetting.Player);
                    Logger.LogInformation("Removed preferred player {PlayerId} from user {UserId}", playerId, userId);
                    return true;
                }
            }
        }

        public async Task<IEnumerable<PuzzleHistory>> GetPuzzleHistoryAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogError("UserId must not be null or empty when calling GetPuzzleHistoryAsync");
                throw new ArgumentException(nameof(userId));
            }

            var history = await (await PuzzleHistoryRepository.GetAllAsync()).Where(h => string.Equals(userId, h.UserId)).ToArrayAsync();
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

        private async Task<UserSettings> GetUserSettingsAsync(string userId)
        {
            return await (await UserSettingsRepository.GetAllAsync()).FirstOrDefaultAsync(s => userId.Equals(s.UserId));
        }

        private async Task CleanUpPlayerAsync(Player player)
        {
            // TODO : Remove the player entity if no users prefer them
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Services
{
    public class PlayerService : IPlayerService
    {
        public IRepository<Player> PlayerRepository { get; }

        public IRepository<TacticsPuzzle> PuzzleRepository { get; }

        public IRepository<UserSettings> UserSettingsRepository { get; }

        public ILogger<PlayerService> Logger { get; }

        public PlayerService(IRepository<Player> playerRepository,
                             IRepository<TacticsPuzzle> puzzleRepository,
                             IRepository<UserSettings> userSettingsRepository,
                             ILogger<PlayerService> logger)
        {
            PlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            PuzzleRepository = puzzleRepository ?? throw new ArgumentNullException(nameof(puzzleRepository));
            UserSettingsRepository = userSettingsRepository ?? throw new ArgumentNullException(nameof(userSettingsRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CleanUpPlayerAsync(int playerId)
        {
            var player = await PlayerRepository.GetAsync(playerId);

            if (player is null ||
                await GetPlayerPuzzleCountAsync(player.Id) > 0 ||
                await UserSettingsRepository.Query(s => s.PreferredPlayers.Any(p => p.PlayerId == playerId)).AnyAsync())
            {
                return;
            }

            Logger.LogInformation("Removing player {PlayerId} because it is no longer used by any puzzle or player", playerId);
            await PlayerRepository.DeleteAsync(playerId);
        }

        public async Task<Player> GetOrAddPlayerAsync(string name, ChessSites site)
        {
            var player = await PlayerRepository.Query(p => name.ToLower().Equals(p.Name.ToLower()) && site == p.Site).FirstOrDefaultAsync();
            if (player is null)
            {
                // TODO: Validate that player exists
                player = await PlayerRepository.AddAsync(new Player(name, site));
                Logger.LogInformation("Added new player {PlayerId}", player.Id);
            }

            return player;
        }

        public async Task<int> GetPlayerPuzzleCountAsync(int playerId) =>
            await PuzzleRepository
                .Query(p => (p.BlackPlayer != null && p.BlackPlayer.Id == playerId) || (p.WhitePlayer != null && p.WhitePlayer.Id == playerId))
                .CountAsync();
    }
}

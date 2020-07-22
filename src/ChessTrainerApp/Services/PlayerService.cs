using System;
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

        public ILogger<PlayerService> Logger { get; }

        public PlayerService(IRepository<Player> playerRepository,
                             IRepository<TacticsPuzzle> puzzleRepository,
                             ILogger<PlayerService> logger)
        {
            PlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            PuzzleRepository = puzzleRepository ?? throw new ArgumentNullException(nameof(puzzleRepository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DeletePlayerAsync(int playerId)
        {
            Logger.LogInformation("Removing player {PlayerId}", playerId);
            await PlayerRepository.DeleteAsync(playerId);
        }

        public async Task<Player> GetOrAddPlayerAsync(string name, ChessSites site)
        {
            var player = await PlayerRepository.Query(p => name.ToLower().Equals(p.Name.ToLower()) && site == p.Site).FirstOrDefaultAsync();
            if (player is null)
            {
                // TODO: Validate that player exists on the given site
                player = await PlayerRepository.AddAsync(new Player(name, site));
                Logger.LogInformation("Added new player {PlayerId}", player.Id);
            }

            return player;
        }
    }
}

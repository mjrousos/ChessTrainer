using System;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using IngestionFunctions.Models;
using IngestionFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

namespace IngestionFunctions
{
    public class GameIngestionFunctions
    {
        private const string PlayerIdQueryKey = "PlayerId";
        private const string PlayerIngestionQueueSettingName = "%PlayerIngestionQueue%";
        private const string StorageConnectionStringSettingName = "StorageConnectionString";

        private IRepository<Player> PlayerRepository { get; }

        private CloudTable GameTable { get; }

        private QueueClient GameQueue { get; }

        private ChessServiceResolver ServiceResolver { get; }

        private ILogger<GameIngestionFunctions> Logger { get; }

        public GameIngestionFunctions(IRepository<Player> playerRepository, CloudTable gameTable, QueueClient gameQueue, ChessServiceResolver serviceResolver, ILogger<GameIngestionFunctions> logger)
        {
            PlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            GameTable = gameTable ?? throw new ArgumentNullException(nameof(gameTable));
            GameQueue = gameQueue ?? throw new ArgumentNullException(nameof(gameQueue));
            ServiceResolver = serviceResolver ?? throw new ArgumentNullException(nameof(serviceResolver));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Logger.LogInformation("Game ingestion functions started");
        }

        [FunctionName("ReviewPlayers")]
        public async Task ReviewPlayers([TimerTrigger("0 0 1 * * *")]TimerInfo timer)
        {
            Logger.LogInformation("Reviewing players for new games");
            foreach (var player in await PlayerRepository.Query(p => p.Site != ChessSites.Other).ToArrayAsync())
            {
                Logger.LogInformation("Checking for new games for player {PlayerId}", player.Id);
                var count = await IngestGamesForPlayerAsync(player);
                Logger.LogInformation("Queued {GameCount} games for ingestion for player {PlayerId}", count, player.Id);
            }

            Logger.LogInformation("Player review done. Will check next at {ScheduleTime}", timer.Schedule.GetNextOccurrence(DateTime.Now));
        }

        [FunctionName("HealthCheck")]
        public async Task<IActionResult> HealthCheck([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            try
            {
                await PlayerRepository.Query().FirstOrDefaultAsync();
            }
            catch (DbException)
            {
                return new ServiceUnavailableObjectResult("Player repository unavailable");
            }

            if (!await GameTable.ExistsAsync())
            {
                return new ServiceUnavailableObjectResult("Most recent game ingested table unavailable");
            }

            if ((await GameQueue.CreateAsync()).Status / 100 != 2)
            {
                return new ServiceUnavailableObjectResult("Ingestion queue unavailable");
            }

            return new OkResult();
        }

        [FunctionName("AddQueuedPlayer")]
        public async Task ProcessQueuedPlayer([QueueTrigger(PlayerIngestionQueueSettingName, Connection = StorageConnectionStringSettingName)] int playerId)
        {
            Logger.LogInformation("Finding games for player {PlayerId}", playerId);

            var player = await PlayerRepository.GetAsync(playerId);
            if (player == null)
            {
                Logger.LogInformation("Player {PlayerId} not found", playerId);
                return;
            }

            var count = await IngestGamesForPlayerAsync(player);
            Logger.LogInformation("Queued {GameCount} games for ingestion for player {PlayerId}", count, player.Id);
        }

        [FunctionName("AddPlayer")]
        public async Task<IActionResult> AddPlayersGames([HttpTrigger(AuthorizationLevel.Function, "put", Route = "AddPlayer")] HttpRequest req)
        {
            var playerIdString = req.Query[PlayerIdQueryKey].ToString();
            if (!int.TryParse(playerIdString, out var playerId))
            {
                Logger.LogInformation("Invalid player ID: [{PlayerId}]", playerIdString);
                return new BadRequestResult();
            }

            var player = await PlayerRepository.GetAsync(playerId);
            if (player == null)
            {
                Logger.LogInformation("Player not found");
                return new NotFoundResult();
            }

            var count = await IngestGamesForPlayerAsync(player);
            Logger.LogInformation("Queued {GameCount} games for ingestion for player {PlayerId}", count, player.Id);

            return new OkObjectResult($"Queued {count} games for player {player.Name}");
        }

        private async Task<int> IngestGamesForPlayerAsync(Player player)
        {
            Logger.LogInformation("Finding games for {PlayerId}", player.Id);

            var mostRecentGame = await GetMostRecentGameAsync(player);
            if (mostRecentGame.HasValue)
            {
                // Add a small buffer to make sure we don't re-process the most recent game.
                mostRecentGame = mostRecentGame.Value.AddMinutes(1);
            }

            var newMostRecentGame = mostRecentGame;
            var ingestedCount = 0;

            await foreach (var game in ServiceResolver(player.Site).GetPlayerGamesAsync(player.Name, mostRecentGame, 50))
            {
                Logger.LogInformation("Found {GamePath}", game.Site);

                if (!newMostRecentGame.HasValue || game.StartDate > newMostRecentGame)
                {
                    newMostRecentGame = game.StartDate;
                }

                var ingestionRequest = new IngestionRequest(game, player.Site, game.Site);
                await QueueIngestionRequestAsync(ingestionRequest);
                ingestedCount++;
            }

            if (newMostRecentGame.HasValue && newMostRecentGame != mostRecentGame)
            {
                await SetMostRecentGameAsync(player, newMostRecentGame.Value);
            }

            return ingestedCount;
        }

        private async Task<DateTimeOffset?> GetMostRecentGameAsync(Player player)
        {
            var retrievalOperation = TableOperation.Retrieve<IngestionRecord>(player.Site.ToString(), player.Name);
            try
            {
                var result = await GameTable.ExecuteAsync(retrievalOperation);

                switch (result.HttpStatusCode)
                {
                    case 404:
                        Logger.LogInformation("No games ingested yet for player {PlayerId}", player.Id);
                        return null;
                    case 200:
                        var ingestionRecord = result.Result as IngestionRecord;
                        if (ingestionRecord == null)
                        {
                            Logger.LogInformation("No games ingested yet for player {PlayerId}", player.Id);
                            return null;
                        }
                        else
                        {
                            Logger.LogInformation("Most recent game ingested for player {PlayerId}: {IngestionDate}", player.Id, ingestionRecord.MostRecentGame);
                            return ingestionRecord.MostRecentGame;
                        }

                    default:
                        Logger.LogError("Unexpected result from ingestion record table query: {StatusCode} {Contents}", result.HttpStatusCode, result.Result);
                        return null;
                }
            }
            catch (StorageException exc)
            {
                Logger.LogError("Unexpected exception from ingestion record table query: {Exception}", exc.ToString());
                throw;
            }
        }

        private async Task SetMostRecentGameAsync(Player player, DateTimeOffset newMostRecentGame)
        {
            var upsertOperation = TableOperation.InsertOrReplace(new IngestionRecord { ChessSite = player.Site.ToString(), Player = player.Name, MostRecentGame = newMostRecentGame });

            try
            {
                var result = await GameTable.ExecuteAsync(upsertOperation);
                if (result.HttpStatusCode / 100 != 2)
                {
                    Logger.LogError("Unexpected result from updating ingestion record table: {StatusCode} {Contents}", result.HttpStatusCode, result.Result);
                }
                else
                {
                    Logger.LogInformation("Updated {PlayerId} most recent ingested game time to {MostRecentTime}", player.Id, newMostRecentGame);
                }
            }
            catch (StorageException exc)
            {
                Logger.LogError("Unexpected exception updating ingestion record table: {Exception}", exc.ToString());
                throw;
            }
        }

        private async Task QueueIngestionRequestAsync(IngestionRequest ingestionRequest)
        {
            try
            {
                await GameQueue.SendMessageAsync(JsonSerializer.Serialize(ingestionRequest));
                Logger.LogInformation("Queued ingestion request for game {GamePath}", ingestionRequest.GameUrl);
            }
            catch (StorageException exc)
            {
                Logger.LogError("Unexpected exception queueing ingestion requst for game {GamePath}: {Exception}", ingestionRequest.GameUrl, exc.ToString());
                throw;
            }
        }
    }
}

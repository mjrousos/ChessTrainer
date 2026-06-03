using System;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using IngestionFunctions.Models;
using IngestionFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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

        private TableClient GameTable { get; }

        private QueueClient GameQueue { get; }

        private ChessServiceResolver ServiceResolver { get; }

        private ILogger<GameIngestionFunctions> Logger { get; }

        public GameIngestionFunctions(IRepository<Player> playerRepository, TableClient gameTable, QueueClient gameQueue, ChessServiceResolver serviceResolver, ILogger<GameIngestionFunctions> logger)
        {
            PlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            GameTable = gameTable ?? throw new ArgumentNullException(nameof(gameTable));
            GameQueue = gameQueue ?? throw new ArgumentNullException(nameof(gameQueue));
            ServiceResolver = serviceResolver ?? throw new ArgumentNullException(nameof(serviceResolver));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Logger.LogInformation("Game ingestion functions started");
        }

        [Function("ReviewPlayers")]
        public async Task ReviewPlayers([TimerTrigger("0 0 1 * * *")] TimerInfo timer)
        {
            Logger.LogInformation("Reviewing players for new games");
            foreach (var player in await PlayerRepository.Query(p => p.Site != ChessSites.Other).ToArrayAsync())
            {
                Logger.LogInformation("Checking for new games for player {PlayerId}", player.Id);
                var count = await IngestGamesForPlayerAsync(player);
                Logger.LogInformation("Queued {GameCount} games for ingestion for player {PlayerId}", count, player.Id);
            }

            Logger.LogInformation("Player review done. Will check next at {ScheduleTime}", timer.ScheduleStatus?.Next);
        }

        [Function("HealthCheck")]
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

            try
            {
                var tableResponse = await GameTable.CreateIfNotExistsAsync();
                if (tableResponse?.GetRawResponse().IsError == true)
                {
                    return new ServiceUnavailableObjectResult("Most recent game ingested table unavailable");
                }
            }
            catch (RequestFailedException exc)
            {
                Logger.LogError("Unexpected exception checking ingestion record table: {Exception}", exc.ToString());
                return new ServiceUnavailableObjectResult("Most recent game ingested table unavailable");
            }

            try
            {
                // CreateIfNotExistsAsync is idempotent: it throws RequestFailedException
                // only on real failures (auth, throttling, network). A non-throwing call —
                // whether the queue was newly created or already existed — means the queue
                // is reachable and healthy, so we don't need to inspect the response.
                await GameQueue.CreateIfNotExistsAsync();
            }
            catch (RequestFailedException exc)
            {
                Logger.LogError("Unexpected exception checking ingestion queue: {Exception}", exc.ToString());
                return new ServiceUnavailableObjectResult("Ingestion queue unavailable");
            }

            return new OkResult();
        }

        [Function("AddQueuedPlayer")]
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

        [Function("AddPlayer")]
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

                var ingestionRequest = new IngestionRequest(game, player.Id, player.Site, game.Site);
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
            try
            {
                var result = await GameTable.GetEntityAsync<IngestionRecord>(player.Site.ToString(), player.Name);
                var ingestionRecord = result.Value;
                Logger.LogInformation("Most recent game ingested for player {PlayerId}: {IngestionDate}", player.Id, ingestionRecord.MostRecentGame);
                return ingestionRecord.MostRecentGame;
            }
            catch (RequestFailedException exc) when (exc.Status == 404)
            {
                Logger.LogInformation("No games ingested yet for player {PlayerId}", player.Id);
                return null;
            }
            catch (RequestFailedException exc)
            {
                Logger.LogError("Unexpected exception from ingestion record table query: {Exception}", exc.ToString());
                throw;
            }
        }

        private async Task SetMostRecentGameAsync(Player player, DateTimeOffset newMostRecentGame)
        {
            var ingestionRecord = new IngestionRecord
            {
                ChessSite = player.Site.ToString(),
                Player = player.Name,
                MostRecentGame = newMostRecentGame
            };

            try
            {
                var response = await GameTable.UpsertEntityAsync(ingestionRecord, TableUpdateMode.Replace);
                if (response.Status / 100 != 2)
                {
                    Logger.LogError("Unexpected result from updating ingestion record table: {StatusCode} {ReasonPhrase}", response.Status, response.ReasonPhrase);
                }
                else
                {
                    Logger.LogInformation("Updated {PlayerId} most recent ingested game time to {MostRecentTime}", player.Id, newMostRecentGame);
                }
            }
            catch (RequestFailedException exc)
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
            catch (RequestFailedException exc)
            {
                Logger.LogError("Unexpected exception queueing ingestion request for game {GamePath}: {Exception}", ingestionRequest.GameUrl, exc.ToString());
                throw;
            }
        }
    }
}

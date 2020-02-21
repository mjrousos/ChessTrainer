using System;
using Azure.Storage.Queues;
using IngestionFunctions.Services;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MjrChess.Engine;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

[assembly: FunctionsStartup(typeof(IngestionFunctions.Startup))]

namespace IngestionFunctions
{
    public class Startup : FunctionsStartup
    {
        private const string PuzzleDbConnectionStringSettingName = "PuzzleDbConnectionString";
        private const string StorageConnectionStringSettingName = "StorageConnectionString";
        private const string GameIngestionQueueSettingName = "GameIngestionQueue";
        private const string GameTableSettingName = "GameTable";
        private const string LichessTokenName = "LichessToken";

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Get configuration
            var config = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            // Add EF.Core and repository services
            var connectionString = config[PuzzleDbConnectionStringSettingName];
            builder.Services.AddChessTrainerData(connectionString);

            // Add table storage service
            var tableClient = CloudStorageAccount.Parse(config[StorageConnectionStringSettingName]).CreateCloudTableClient();
            var table = tableClient.GetTableReference(config[GameTableSettingName]);
            table.CreateIfNotExists();
            builder.Services.AddSingleton(table);

            // Add chess site services
            builder.Services.AddHttpClient<LiChessService>(client =>
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config[LichessTokenName]}");
            });
            builder.Services.AddHttpClient<ChessComService>();
            builder.Services.AddTransient<ChessServiceResolver>(sp => site =>
                site switch
                {
                    ChessSites.ChessCom => sp.GetRequiredService<ChessComService>(),
                    ChessSites.LiChess => sp.GetRequiredService<LiChessService>(),
                    _ => throw new ArgumentException($"Unsupported chess site {site}", nameof(site))
                });

            // Add queue storage service
            var queueClient = new QueueClient(config[StorageConnectionStringSettingName], config[GameIngestionQueueSettingName]);
            queueClient.Create();
            builder.Services.AddSingleton(queueClient);

            builder.Services.AddTransient<ChessEngine>();
        }
    }
}

using System;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using IngestionFunctions;
using IngestionFunctions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MjrChess.Engine;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;

const string PuzzleDbConnectionStringSettingName = "PuzzleDbConnectionString";
const string StorageConnectionStringSettingName = "StorageConnectionString";
const string GameIngestionQueueSettingName = "GameIngestionQueue";
const string GameTableSettingName = "GameTable";
const string LichessTokenName = "LichessToken";

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

var config = builder.Configuration;
builder.Services.AddChessTrainerData(config[PuzzleDbConnectionStringSettingName]!);

var tableServiceClient = new TableServiceClient(config[StorageConnectionStringSettingName]!);
var tableClient = tableServiceClient.GetTableClient(config[GameTableSettingName]!);
await tableClient.CreateIfNotExistsAsync();
builder.Services.AddSingleton(tableClient);

builder.Services.AddHttpClient<LiChessService>(client =>
{
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config[LichessTokenName]}");
})
    .AddPolicyHandler(HttpPolicies.RetryPolicy)
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy);

builder.Services.AddHttpClient<ChessComService>();
builder.Services.AddTransient<ChessServiceResolver>(sp => site => site switch
{
    ChessSites.ChessCom => sp.GetRequiredService<ChessComService>(),
    ChessSites.LiChess => sp.GetRequiredService<LiChessService>(),
    _ => throw new ArgumentException($"Unsupported chess site {site}", nameof(site))
});

var queueClient = new QueueClient(config[StorageConnectionStringSettingName]!, config[GameIngestionQueueSettingName]!);
await queueClient.CreateIfNotExistsAsync();
builder.Services.AddSingleton(queueClient);

builder.Services.AddTransient<ChessEngine>();

await builder.Build().RunAsync();

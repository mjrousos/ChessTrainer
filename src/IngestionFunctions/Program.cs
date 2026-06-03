using System;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Queues;
using IngestionFunctions;
using IngestionFunctions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
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
builder.Services.AddChessTrainerData(RequireSetting(config, PuzzleDbConnectionStringSettingName));

// Storage auth supports two modes (matches the Functions runtime's identity-based
// connection conventions for the [QueueTrigger] binding):
//   1. Identity-based: set StorageConnectionString__tableServiceUri AND
//      StorageConnectionString__queueServiceUri to the service URIs (e.g.,
//      https://<account>.table.core.windows.net). Auth uses DefaultAzureCredential,
//      which picks up az login / Visual Studio / managed identity in production.
//      The runtime principal needs Storage Table Data Contributor + Storage Queue
//      Data Contributor on the account.
//   2. Connection string: set StorageConnectionString to a full connection string,
//      or "UseDevelopmentStorage=true" for Azurite.
// Identity-based mode wins when both are configured.
var tableServiceUri = config[$"{StorageConnectionStringSettingName}:tableServiceUri"];
var queueServiceUri = config[$"{StorageConnectionStringSettingName}:queueServiceUri"];

TableServiceClient tableServiceClient;
QueueClient queueClient;

if (!string.IsNullOrWhiteSpace(tableServiceUri) && !string.IsNullOrWhiteSpace(queueServiceUri))
{
    var queueName = RequireSetting(config, GameIngestionQueueSettingName);
    var credential = new DefaultAzureCredential();
    tableServiceClient = new TableServiceClient(new Uri(tableServiceUri), credential);
    queueClient = new QueueClient(
        new Uri($"{queueServiceUri.TrimEnd('/')}/{queueName}"),
        credential);
}
else
{
    var storageConnectionString = config[StorageConnectionStringSettingName];
    if (string.IsNullOrWhiteSpace(storageConnectionString))
    {
        throw new InvalidOperationException(
            $"Storage auth not configured. Set either '{StorageConnectionStringSettingName}' " +
            $"(connection string, including 'UseDevelopmentStorage=true' for Azurite), or both " +
            $"'{StorageConnectionStringSettingName}__tableServiceUri' and " +
            $"'{StorageConnectionStringSettingName}__queueServiceUri' (managed identity).");
    }

    var queueName = RequireSetting(config, GameIngestionQueueSettingName);
    tableServiceClient = new TableServiceClient(storageConnectionString);
    queueClient = new QueueClient(storageConnectionString, queueName);
}

var tableClient = tableServiceClient.GetTableClient(RequireSetting(config, GameTableSettingName));
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

await queueClient.CreateIfNotExistsAsync();
builder.Services.AddSingleton(queueClient);

builder.Services.AddTransient<ChessEngine>();

await builder.Build().RunAsync();

// Reads a required configuration setting and throws InvalidOperationException with a
// descriptive message if it is missing, empty, or whitespace-only, instead of letting
// downstream code fail later with an opaque NRE or SDK-level error.
static string RequireSetting(IConfiguration configuration, string name)
{
    var value = configuration[name];
    return !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new InvalidOperationException(
            $"Required configuration setting '{name}' is missing or empty. " +
            $"Set it in local.settings.json (locally) or in the Function App's application settings (in Azure).");
}

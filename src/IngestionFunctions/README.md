# IngestionFunctions

Azure Functions app responsible for discovering chess games for tracked players (Lichess + Chess.com) and queueing them for puzzle ingestion. Runs on **.NET 10** under the **Azure Functions v4 isolated worker** model.

## Functions

| Name | Trigger | Notes |
|---|---|---|
| `ReviewPlayers` | Timer (`0 0 1 * * *` — daily at 01:00 UTC) | Iterates every tracked player and queues new games. |
| `HealthCheck` | HTTP `GET /api/HealthCheck` | Verifies DB, table, and queue connectivity. |
| `AddPlayer` | HTTP `PUT /api/AddPlayer?PlayerId={id}` | Queues new games for a single player by ID. |
| `AddQueuedPlayer` | Queue (`%PlayerIngestionQueue%`) | Consumes player IDs from the queue and ingests their games. |

## Running locally

### Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0.x | <https://dotnet.microsoft.com/download> |
| Azure Functions Core Tools | v4 | `npm i -g azure-functions-core-tools@4 --unsafe-perm true` or `winget install Microsoft.AzureFunctionsCoreTools` |
| Docker | any recent | <https://www.docker.com/products/docker-desktop/> — needed for Azurite + SQL Server |

> Don't have Docker? You can swap Azurite for the [Azurite VS Code extension](https://marketplace.visualstudio.com/items?itemName=Azurite.azurite) and SQL Server for SQL Server LocalDB / Express — adjust the connection strings in step 2 accordingly.

### 1. Start the backing services

```powershell
# Azurite — provides queue + table emulation on localhost
docker run -d --name azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 `
  mcr.microsoft.com/azure-storage/azurite

# SQL Server 2022 (free developer edition; ARM Macs: use mcr.microsoft.com/azure-sql-edge instead)
docker run -d --name puzzle-sql -p 1433:1433 `
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Local!Password123 `
  mcr.microsoft.com/mssql/server:2022-latest
```

Verify both are healthy:

```powershell
docker ps --filter "name=azurite" --filter "name=puzzle-sql"
```

### 2. Create `local.settings.json`

This file is **gitignored** — create it once in `src/IngestionFunctions/`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "StorageConnectionString": "UseDevelopmentStorage=true",
    "GameTable": "ingestionrecords",
    "GameIngestionQueue": "game-ingestion",
    "PlayerIngestionQueue": "player-ingestion",
    "PuzzleDbConnectionString": "Server=localhost,1433;Database=PuzzleDb;User Id=sa;Password=Local!Password123;TrustServerCertificate=true;",
    "LichessToken": "<your-lichess-token-or-leave-empty>"
  }
}
```

`UseDevelopmentStorage=true` is the well-known shortcut that points `Azure.Data.Tables` and `Azure.Storage.Queues` at the default Azurite endpoints. The `Program.cs` bootstrap calls `CreateIfNotExistsAsync()` on both the table and queue, so you don't need to pre-create them.

### 3. Bootstrap the `PuzzleDb` schema

EF Core migrations live in `ChessTrainerApp` (still on `netcoreapp3.1` until that project is upgraded). Two options:

- **Quick test path**: connect to the local SQL container (e.g., with Azure Data Studio or `sqlcmd`) and create the `PuzzleDb` database plus a `Players` table with one test row. That's enough for `HealthCheck` and `AddPlayer` to exercise the EF Core 10 layer end-to-end.
- **Proper path**: once `ChessTrainerApp` is upgraded, run `dotnet ef database update --project src/ChessTrainerApp` against the local SQL container — this materializes the full schema.

### 4. Run the function host

```powershell
cd src/IngestionFunctions
func start
```

You should see the four functions enumerated:

```text
Functions:
  AddPlayer:        [PUT] http://localhost:7071/api/AddPlayer
  AddQueuedPlayer:  queueTrigger
  HealthCheck:      [GET] http://localhost:7071/api/HealthCheck
  ReviewPlayers:    timerTrigger
```

### 5. Invoke each function

```powershell
# HealthCheck — local host disables function-key enforcement
curl http://localhost:7071/api/HealthCheck

# AddPlayer — requires ?PlayerId=N in the query string
curl -X PUT "http://localhost:7071/api/AddPlayer?PlayerId=1"

# ReviewPlayers — trigger the timer manually via the admin endpoint
curl -X POST http://localhost:7071/admin/functions/ReviewPlayers `
  -H "Content-Type: application/json" -d "{}"

# AddQueuedPlayer — drop an integer payload onto the player-ingestion queue.
# Easiest: use Azure Storage Explorer (free) pointed at "Local & Attached"
# > Storage Accounts > (Emulator - Default Ports) > Queues > player-ingestion.
# Programmatic alternative with the Azure CLI:
az storage message put `
  --queue-name player-ingestion `
  --content (echo -n "1" | base64) `
  --connection-string "UseDevelopmentStorage=true"
```

## Debugging

Attach your debugger to the **`func.exe`** process (Visual Studio: *Debug > Attach to Process*; VS Code: the "Attach to .NET Functions" launch profile).

## Common gotchas

- **`func start` prompts for a worker runtime** → your `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` isn't being picked up. Make sure `local.settings.json` lives in `src/IngestionFunctions/` (not the repo root) and that you run `func start` from inside that folder.
- **`Connection refused` on storage** → Azurite isn't running. `docker start azurite` and retry.
- **`Login failed for user 'sa'`** → the SQL container is still initializing on first boot (can take ~30s). Wait, then retry.
- **Non-default Azurite ports** → the `UseDevelopmentStorage=true` shortcut only works for the default port set (10000/10001/10002). If you change them, use the full `DefaultEndpointsProtocol=…;AccountName=devstoreaccount1;AccountKey=…;BlobEndpoint=http://127.0.0.1:NNNNN/devstoreaccount1;…` string instead.

## Stopping the environment

```powershell
docker stop azurite puzzle-sql
# Or remove entirely:
docker rm -f azurite puzzle-sql
```

## Related docs

- [`host.json`](host.json) — Functions host configuration.
- [`.github/upgrades/scenarios/dotnet-version-upgrade/`](../../.github/upgrades/scenarios/dotnet-version-upgrade/) — record of the .NET 10 upgrade that produced this project's current shape (in-process → isolated worker, Cosmos.Table → Azure.Data.Tables, etc.).
- [Azure Functions isolated worker model](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)

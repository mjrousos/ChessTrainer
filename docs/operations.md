# Operations

## Health-check endpoints

The Blazor app (`ChessTrainerApp`) exposes two anonymous health-check endpoints.

| Endpoint | Purpose | Checks run |
|----------|---------|------------|
| `GET /healthz` | **Liveness** — is the process alive? | None (returns 200 as long as the process responds). |
| `GET /readyz` | **Readiness** — can the app serve traffic? | EF Core `PuzzleDbContext` connectivity + Azure Storage queue *(opt-in)*. Returns 503 when any check fails. |

### Liveness (`/healthz`)

Used by App Service health-check path (`/healthz`) and Kubernetes liveness probes.
A 200 means the process is running; restart if it stops responding.
No database round-trip is made, so this probe is extremely cheap.

**Azure App Service**: set *Health check path* to `/healthz` in the App Service configuration.

### Readiness (`/readyz`)

Used by load-balancer / Kubernetes readiness probes.
Returns 200 only when all `ready`-tagged checks pass.
Returns 503 with a plain-text body (`Unhealthy` or `Degraded`) when any dependency is unavailable. The default response writer does not include per-check detail; configure a custom `HealthCheckOptions.ResponseWriter` (e.g. the [AspNetCore.HealthChecks.UI.Client](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks) writer) if you need structured output.

The probe currently includes the following `"ready"`-tagged checks:

| Check | Description | Required config |
|-------|-------------|-----------------|
| EF Core `PuzzleDbContext` | SQL Server reachable via `PuzzleDbContext` | `ConnectionStrings:PuzzleDatabase` (always required) |
| Azure Storage queue | Game-ingestion queue reachable | `StorageConnectionString` *(opt-in: check is skipped when not set)* |

The Azure Storage queue check uses the same `StorageConnectionString` configuration key as the companion `IngestionFunctions` project. Set it to `UseDevelopmentStorage=true` (Azurite) or a full Azure Storage connection string to enable the check. Optionally override the queue name via `GameIngestionQueue` (defaults to `games`).

### Adding new checks

Tag new checks `"ready"` to include them in the readiness probe:

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<PuzzleDbContext>(tags: new[] { "ready" })
    .AddUrlGroup(new Uri("https://lichess.org"), tags: new[] { "ready" }); // example
```

Liveness checks (no tag or a different tag) are excluded from both probes by the predicate filter in `Startup.cs`.

# Operations

## Health-check endpoints

The Blazor app (`ChessTrainerApp`) exposes two anonymous health-check endpoints.

| Endpoint | Purpose | Checks run |
|----------|---------|------------|
| `GET /healthz` | **Liveness** — is the process alive? | None (returns 200 as long as the process responds). |
| `GET /readyz` | **Readiness** — can the app serve traffic? | EF Core `PuzzleDbContext` connectivity (tagged `ready`). Returns 503 when any check fails. |

### Liveness (`/healthz`)

Used by App Service health-check path (`/healthz`) and Kubernetes liveness probes.
A 200 means the process is running; restart if it stops responding.
No database round-trip is made, so this probe is extremely cheap.

**Azure App Service**: set *Health check path* to `/healthz` in the App Service configuration.

### Readiness (`/readyz`)

Used by load-balancer / Kubernetes readiness probes.
Returns 200 only when all `ready`-tagged checks pass (currently: SQL Server reachable via `PuzzleDbContext`).
Returns 503 with a plain-text body (`Unhealthy`) when any dependency is unavailable. The default response writer does not include per-check detail; configure a custom `HealthCheckOptions.ResponseWriter` (e.g. the [AspNetCore.HealthChecks.UI.Client](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks) writer) if you need structured output.

### Adding new checks

Tag new checks `"ready"` to include them in the readiness probe:

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<PuzzleDbContext>(tags: new[] { "ready" })
    .AddUrlGroup(new Uri("https://lichess.org"), tags: new[] { "ready" }); // example
```

Liveness checks (no tag or a different tag) are excluded from both probes by the predicate filter in `Startup.cs`.

# Upgrade Options — ChessTrainer

Assessment: 5 in-scope SDK-style projects (`netstandard2.0`, `netstandard2.1`, `netcoreapp3.0`, `netcoreapp3.1`) targeting `net10.0`; 3 incompatible packages, 6 package upgrades, and 7 API compatibility findings.

## Strategy

### Upgrade Strategy
The in-scope upgrade is a short dependency chain (`Engine` → `ChessTrainer.Common` → `ChessTrainer.Data` → `IngestionFunctions`) plus a standalone test project; leaf-first validation is the recommended default.

| Value | Description |
|-------|-------------|
| All-at-Once | Upgrade all projects simultaneously in a single atomic pass. |
| **Bottom-Up** (selected) | Upgrade leaf-node libraries first, then work upward through the dependency graph tier by tier with validation at each tier. |
| Top-Down | Upgrade entry-point applications first, temporarily multi-targeting shared libraries so the solution stays buildable throughout. |

## Compatibility

### Unsupported Packages
Assessment reports 3 incompatible packages and required package replacements for the Azure Functions v3 in-process to v4 isolated worker migration.

| Value | Description |
|-------|-------------|
| **Resolve Inline** (selected) | Research and resolve each incompatible package within the same task by removing old references, adding replacements, or rewriting consuming code. |
| Defer Resolution | Make the project compile without unresolved packages by using minimal stubs, then create follow-up tasks for real replacements. |
| Compatibility Mode | Keep a .NET Framework reference with compatibility packages and suppress NU1701 only when consuming code does not directly call package APIs. |

### Unsupported API Handling
Assessment reports 3 source-incompatible `TimeSpan.FromSeconds` findings and 4 behavioral `HttpContent` / `Uri` findings.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve every API change in the same task, including complex ones, leaving no deferred stub work. |
| Defer Complex Changes | Apply simple replacements inline and generate minimal compilable stubs only for complex changes that need follow-up subtasks. |

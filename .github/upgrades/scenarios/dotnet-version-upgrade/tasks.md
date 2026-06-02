# .NET Version Upgrade Progress

## Overview

Upgrade the IngestionFunctions dependency chain to `net10.0` using a Bottom-Up dependency-first strategy. Shared libraries will single-target .NET 10, the Functions app will move to v4 isolated worker, and the out-of-scope ChessTrainerApp breakage will be documented at final validation.

**Progress**: 2/7 tasks complete <progress value="29" max="100"></progress> 29%

## Tasks

- ✅ 01-upgrade-foundations: Prepare shared build settings for net10.0 ([Content](tasks/01-upgrade-foundations/task.md), [Progress](tasks/01-upgrade-foundations/progress-details.md))
- ✅ 02-engine: Retarget the Engine library ([Content](tasks/02-engine/task.md), [Progress](tasks/02-engine/progress-details.md))
- 🔲 03-common-models: Retarget shared common models
- 🔲 04-data-access: Upgrade EF Core and AutoMapper data layer
- 🔲 05-ingestion-functions: Migrate Functions app to isolated worker
- 🔲 06-ingestion-test: Retarget the Functions test project
- 🔲 07-solution-validation: Validate upgrade scope and document hand-off

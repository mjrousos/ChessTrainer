# .NET Version Upgrade Progress

## Overview

Upgrade the IngestionFunctions dependency chain to `net10.0` using a Bottom-Up dependency-first strategy. Shared libraries will single-target .NET 10, the Functions app will move to v4 isolated worker, and the out-of-scope ChessTrainerApp breakage will be documented at final validation.

**Progress**: 7/7 tasks complete <progress value="100" max="100"></progress> 100%

## Tasks

- ✅ 01-upgrade-foundations: Prepare shared build settings for net10.0 ([Content](tasks/01-upgrade-foundations/task.md), [Progress](tasks/01-upgrade-foundations/progress-details.md))
- ✅ 02-engine: Retarget the Engine library ([Content](tasks/02-engine/task.md), [Progress](tasks/02-engine/progress-details.md))
- ✅ 03-common-models: Retarget shared common models ([Content](tasks/03-common-models/task.md), [Progress](tasks/03-common-models/progress-details.md))
- ✅ 04-data-access: Upgrade EF Core and AutoMapper data layer ([Content](tasks/04-data-access/task.md), [Progress](tasks/04-data-access/progress-details.md))
- ✅ 05-ingestion-functions: Migrate Functions app to isolated worker ([Content](tasks/05-ingestion-functions/task.md), [Progress](tasks/05-ingestion-functions/progress-details.md))
- ✅ 06-ingestion-test: Retarget the Functions test project ([Content](tasks/06-ingestion-test/task.md), [Progress](tasks/06-ingestion-test/progress-details.md))
- ✅ 07-solution-validation: Validate upgrade scope and document hand-off ([Content](tasks/07-solution-validation/task.md), [Progress](tasks/07-solution-validation/progress-details.md))

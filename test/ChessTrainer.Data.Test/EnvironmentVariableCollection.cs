using Xunit;

namespace ChessTrainer.Data.Test
{
    // Disables xUnit parallelization for tests that mutate process-global state
    // (the PuzzleDbConnectionString environment variable).
    [CollectionDefinition(nameof(EnvironmentVariableCollection), DisableParallelization = true)]
    public class EnvironmentVariableCollection
    {
    }
}

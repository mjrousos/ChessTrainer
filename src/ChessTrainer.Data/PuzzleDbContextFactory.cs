using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MjrChess.Trainer.Data
{
    /// <summary>
    /// Design-time factory used by the <c>dotnet ef</c> tooling to construct a
    /// <see cref="PuzzleDbContext"/> without needing to spin up a startup project.
    ///
    /// The connection string is read from the <c>PuzzleDbConnectionString</c>
    /// environment variable (the same name used by the application hosts).
    /// When that variable is not set, the factory falls back to the local
    /// SQL Server container connection string documented in
    /// <c>src/IngestionFunctions/README.md</c>.
    /// </summary>
    public class PuzzleDbContextFactory : IDesignTimeDbContextFactory<PuzzleDbContext>
    {
        private const string LocalSqlConnectionString =
            "Server=localhost,1433;Database=PuzzleDb;User Id=sa;Password=Local!Password123;TrustServerCertificate=true;";

        public PuzzleDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("PuzzleDbConnectionString")
                ?? LocalSqlConnectionString;

            var options = new DbContextOptionsBuilder<PuzzleDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new PuzzleDbContext(options);
        }
    }
}

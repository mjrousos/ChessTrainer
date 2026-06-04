using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MjrChess.Trainer.Data;
using Xunit;

namespace ChessTrainer.Data.Test
{
    [Collection(nameof(EnvironmentVariableCollection))]
    public class PuzzleDbContextFactoryTests
    {
        private const string EnvVarName = "PuzzleDbConnectionString";

        private const string ExpectedFallbackConnectionString =
            "Server=localhost,1433;Database=PuzzleDb;User Id=sa;Password=Local!Password123;TrustServerCertificate=true;";

        private static readonly string[] ExpectedMigrations = new[]
        {
            "20200128200837_InitialCreate",
            "20200129194638_AddIncorrectMoveToTacticsPuzzle",
            "20200130041453_AddSiteToTacticsPuzzle",
            "20200131203321_AllowNullIncorrectMoves",
            "20200206223217_AddSetupMoveToTacticsPuzzle",
            "20200526205648_RemovePuzzlePieceType",
            "20200526214301_TacticsHaveNamesAndAssociatedId",
        };

        [Fact]
        public void CreateDbContext_ReturnsSqlServerBackedContext()
        {
            WithEnvironmentVariable(EnvVarName, null, () =>
            {
                var factory = new PuzzleDbContextFactory();
                using var context = factory.CreateDbContext(Array.Empty<string>());

                Assert.NotNull(context);
                Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
            });
        }

        [Fact]
        public void CreateDbContext_UsesEnvironmentVariableWhenSet()
        {
            const string Sentinel =
                "Server=env-host;Database=EnvDb;User Id=env;Password=env-secret;TrustServerCertificate=true;";

            WithEnvironmentVariable(EnvVarName, Sentinel, () =>
            {
                var factory = new PuzzleDbContextFactory();
                using var context = factory.CreateDbContext(Array.Empty<string>());

                AssertConnectionStringsEquivalent(Sentinel, context.Database.GetConnectionString());
            });
        }

        [Fact]
        public void CreateDbContext_FallsBackToLocalConnectionStringWhenEnvVarUnset()
        {
            WithEnvironmentVariable(EnvVarName, null, () =>
            {
                var factory = new PuzzleDbContextFactory();
                using var context = factory.CreateDbContext(Array.Empty<string>());

                AssertConnectionStringsEquivalent(ExpectedFallbackConnectionString, context.Database.GetConnectionString());
            });
        }

        [Fact]
        public void Context_DiscoversMigrationsInDataAssembly()
        {
            WithEnvironmentVariable(EnvVarName, null, () =>
            {
                var factory = new PuzzleDbContextFactory();
                using var context = factory.CreateDbContext(Array.Empty<string>());

                var migrations = context.Database.GetMigrations().ToArray();

                Assert.Equal(ExpectedMigrations.OrderBy(m => m), migrations.OrderBy(m => m));

                // Regression guard: the migrations must physically live in the
                // ChessTrainer.Data assembly. If somebody re-introduces the
                // MigrationsAssembly("MjrChess.Trainer") workaround or forgets to
                // move a migration file, this assertion will fail loudly.
                var migrationsAssembly = context.GetService<IMigrationsAssembly>();
                var dataAssemblyName = typeof(PuzzleDbContext).Assembly.GetName().Name;

                Assert.Equal(dataAssemblyName, migrationsAssembly.Assembly.GetName().Name);

                foreach (var migrationType in migrationsAssembly.Migrations.Values)
                {
                    Assert.Equal(dataAssemblyName, migrationType.Assembly.GetName().Name);
                    Assert.Equal("MjrChess.Trainer.Data.Migrations", migrationType.Namespace);
                }
            });
        }

        private static void AssertConnectionStringsEquivalent(string expected, string? actual)
        {
            Assert.NotNull(actual);
            var expectedBuilder = new SqlConnectionStringBuilder(expected);
            var actualBuilder = new SqlConnectionStringBuilder(actual);

            Assert.Equal(expectedBuilder.DataSource, actualBuilder.DataSource);
            Assert.Equal(expectedBuilder.InitialCatalog, actualBuilder.InitialCatalog);
            Assert.Equal(expectedBuilder.UserID, actualBuilder.UserID);
            Assert.Equal(expectedBuilder.Password, actualBuilder.Password);
            Assert.Equal(expectedBuilder.TrustServerCertificate, actualBuilder.TrustServerCertificate);
        }

        private static void WithEnvironmentVariable(string name, string? value, Action action)
        {
            var original = Environment.GetEnvironmentVariable(name);
            try
            {
                Environment.SetEnvironmentVariable(name, value);
                action();
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, original);
            }
        }
    }
}

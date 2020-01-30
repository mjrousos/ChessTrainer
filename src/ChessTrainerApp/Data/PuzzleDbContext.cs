using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class PuzzleDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; } = default!;

        public DbSet<PuzzleHistory> PuzzleHistories { get; set; } = default!;

        public DbSet<TacticsPuzzle> Puzzles { get; set; } = default!;

        public DbSet<UserSettings> UserSettings { get; set; } = default!;

        public DbSet<UserSettingsXPlayer> UserSettingsXPlayers { get; set; } = default!;

        public PuzzleDbContext(DbContextOptions<PuzzleDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Player configuration
            modelBuilder.Entity<Player>()
                .Property(p => p.Name)
                .IsRequired();

            // Puzzle history configuration
            modelBuilder.Entity<PuzzleHistory>()
                .Property(h => h.UserId)
                .IsRequired();

            modelBuilder.Entity<PuzzleHistory>()
                .HasOne(h => h.Puzzle)
                .WithMany(p => p.History)
                .IsRequired();

            // TacticsPuzzle configuration
            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.Position)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .Ignore(p => p.Solution);

            modelBuilder.Entity<TacticsPuzzle>()
                .Ignore(p => p.IncorrectMove);

            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.PieceMoved)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.MovedFrom)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.MovedTo)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .HasMany(p => p.History)
                .WithOne(h => h.Puzzle);

            // User settings configuration
            modelBuilder.Entity<UserSettings>()
                .Property(s => s.UserId)
                .IsRequired();

            modelBuilder.Entity<UserSettings>()
                .HasMany(s => s.PreferredPlayers)
                .WithOne();

            // User settings x Players join configuration
            modelBuilder.Entity<UserSettingsXPlayer>()
                .HasKey(x => new { x.UserSettingsId, x.PlayerId });

            modelBuilder.Entity<UserSettingsXPlayer>()
                .HasOne(x => x.UserSettings)
                .WithMany(s => s.PreferredPlayers)
                .HasForeignKey(x => x.UserSettingsId);

            modelBuilder.Entity<UserSettingsXPlayer>()
                .HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId);

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                .HasData(
                    new Player("Hustler") { Id = 1 },
                    new Player("Noobie") { Id = 2 });

            modelBuilder.Entity<TacticsPuzzle>()
                .HasData(
                    new
                    {
                        Id = 1,
                        Position = "r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR w KQkq - 4 4",
                        CreatedDate = DateTimeOffset.Now,
                        LastModifiedDate = DateTimeOffset.Now,
                        PieceMoved = Engine.Models.ChessPieces.WhiteQueen,
                        MovedFrom = "f3",
                        MovedTo = "f7",
                        IncorrectPieceMoved = Engine.Models.ChessPieces.WhitePawn,
                        IncorrectMovedFrom = "d2",
                        IncorrectMovedTo = "d4",
                        WhitePlayerId = 1,
                        BlackPlayerId = 2,
                        GameDate = new DateTimeOffset(2015, 2, 7, 0, 0, 0, TimeSpan.Zero)
                    });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var updateTime = DateTimeOffset.Now;
            foreach (var change in ChangeTracker.Entries<IEntity>())
            {
                switch (change.State)
                {
                    case EntityState.Added:
                        change.Entity.CreatedDate = change.Entity.LastModifiedDate = updateTime;
                        break;
                    case EntityState.Modified:
                        change.Entity.LastModifiedDate = updateTime;
                        break;
                }
            }
        }
    }
}

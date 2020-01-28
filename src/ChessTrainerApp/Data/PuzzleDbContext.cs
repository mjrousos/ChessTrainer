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
                .HasMany(s => s.PreferredPlayers)
                .WithOne();

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TacticsPuzzle>()
                .HasData(
                    new TacticsPuzzle("r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR w KQkq - 4 4")
                    {
                        Id = 1,
                        Solution = new Engine.Models.Move(
                            Engine.Models.ChessPieces.WhiteQueen,
                            new Engine.Models.BoardPosition("f3"),
                            new Engine.Models.BoardPosition("f7")),
                        BlackPlayer = "Noobie",
                        WhitePlayer = "Hustler",
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
            foreach (var change in ChangeTracker.Entries<EntityBase>())
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

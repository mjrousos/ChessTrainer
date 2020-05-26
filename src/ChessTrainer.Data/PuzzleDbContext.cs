using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MjrChess.Trainer.Data.Models;

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
                .Property(p => p.MovedFrom)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.MovedTo)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.SetupMovedFrom)
                .IsRequired();

            modelBuilder.Entity<TacticsPuzzle>()
                .Property(p => p.SetupMovedTo)
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
                    new Player("Hustler", Trainer.Models.ChessSites.Other) { Id = 1, CreatedDate = DateTimeOffset.Now, LastModifiedDate = DateTimeOffset.Now },
                    new Player("Noobie", Trainer.Models.ChessSites.Other) { Id = 2, CreatedDate = DateTimeOffset.Now, LastModifiedDate = DateTimeOffset.Now },
                    new Player("Vini700", Trainer.Models.ChessSites.LiChess) { Id = 3, CreatedDate = DateTimeOffset.Now, LastModifiedDate = DateTimeOffset.Now },
                    new Player("aupoil", Trainer.Models.ChessSites.LiChess) { Id = 4, CreatedDate = DateTimeOffset.Now, LastModifiedDate = DateTimeOffset.Now },
                    new Player("toskekg", Trainer.Models.ChessSites.LiChess) { Id = 5, CreatedDate = DateTimeOffset.Now, LastModifiedDate = DateTimeOffset.Now },
                    new Player("wolfwolf", Trainer.Models.ChessSites.LiChess) { Id = 6, CreatedDate = DateTimeOffset.Now, LastModifiedDate = DateTimeOffset.Now });

            modelBuilder.Entity<TacticsPuzzle>()
                .HasData(
                    new
                    {
                        Id = 1,
                        Position = "rnbqk1nr/pppp1ppp/8/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR b KQkq - 3 3",
                        CreatedDate = DateTimeOffset.Now,
                        LastModifiedDate = DateTimeOffset.Now,
                        PieceMoved = Engine.Models.ChessPieces.WhiteQueen,
                        MovedFrom = "f3",
                        MovedTo = "f7",
                        SetupPieceMoved = Engine.Models.ChessPieces.BlackKnight,
                        SetupMovedFrom = "b8",
                        SetupMovedTo = "c6",
                        WhitePlayerId = 1,
                        BlackPlayerId = 2,
                        GameDate = new DateTimeOffset(2015, 2, 7, 0, 0, 0, TimeSpan.Zero)
                    },
                    new
                    {
                        Id = 2,
                        Position = "r3r1k1/ppp2pp1/2n4p/3q4/3Pb3/B1P2N1P/P2Q1PP1/R3R1K1 w - - 4 16",
                        CreatedDate = DateTimeOffset.Now,
                        LastModifiedDate = DateTimeOffset.Now,
                        PieceMoved = Engine.Models.ChessPieces.BlackKnight,
                        MovedFrom = "c6",
                        MovedTo = "a5",
                        SetupPieceMoved = Engine.Models.ChessPieces.WhiteRook,
                        SetupMovedFrom = "e1",
                        SetupMovedTo = "e3",
                        WhitePlayerId = 3,
                        BlackPlayerId = 4,
                        GameDate = new DateTimeOffset(2016, 8, 8, 0, 0, 0, TimeSpan.Zero),
                        Site = "lichess.org",
                        GameUrl = "https://lichess.org/3piQphpY"
                    },
                    new
                    {
                        Id = 3,
                        Position = "r2q1rk1/1pp1b1pp/p7/4pp2/2PnB1P1/3PB2P/PP1Q1P2/R3K2R w KQ - 0 15",
                        CreatedDate = DateTimeOffset.Now,
                        LastModifiedDate = DateTimeOffset.Now,
                        PieceMoved = Engine.Models.ChessPieces.BlackBishop,
                        MovedFrom = "e7",
                        MovedTo = "b4",
                        SetupPieceMoved = Engine.Models.ChessPieces.WhitePawn,
                        SetupMovedFrom = "g4",
                        SetupMovedTo = "f5",
                        IncorrectPieceMoved = Engine.Models.ChessPieces.BlackRook,
                        IncorrectMovedFrom = "f8",
                        IncorrectMovedTo = "f5",
                        WhitePlayerId = 5,
                        BlackPlayerId = 6,
                        GameDate = new DateTimeOffset(2016, 10, 7, 0, 0, 0, TimeSpan.Zero),
                        Site = "lichess.org",
                        GameUrl = "https://lichess.org/HjVhr1Dn"
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

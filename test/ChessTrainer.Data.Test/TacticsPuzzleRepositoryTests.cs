using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Models;
using Xunit;

namespace ChessTrainer.Data.Test
{
    public class TacticsPuzzleRepositoryTests
    {
        [Fact]
        public async Task Query_WhenPuzzleMatchesFilter_ReturnsPuzzleWithHistory()
        {
            using var context = CreateContext();
            var mapper = CreateMapper(context);

            var dataPuzzle = CreateDataPuzzle(associatedPlayerId: 41);
            context.Puzzles.Add(dataPuzzle);
            context.PuzzleHistories.Add(new MjrChess.Trainer.Data.Models.PuzzleHistory
            {
                UserId = "user-1",
                Puzzle = dataPuzzle,
                Solved = true,
            });

            await context.SaveChangesAsync();

            var repository = new TacticsPuzzleRepository(
                context,
                mapper,
                NullLogger<EFRepository<MjrChess.Trainer.Data.Models.TacticsPuzzle, TacticsPuzzle>>.Instance);

            var puzzle = await repository.Query(p => p.AssociatedPlayerId == 41).SingleAsync();

            var history = Assert.Single(puzzle.History);
            Assert.Equal("user-1", history.UserId);
            Assert.True(history.Solved);
        }

        [Fact]
        public async Task UpdateAsync_WhenPuzzleHistorySolvedStateChanges_PersistsUpdate()
        {
            using var context = CreateContext();
            var mapper = CreateMapper(context);

            var dataPuzzle = CreateDataPuzzle(associatedPlayerId: 99);
            context.Puzzles.Add(dataPuzzle);
            context.PuzzleHistories.Add(new MjrChess.Trainer.Data.Models.PuzzleHistory
            {
                UserId = "user-2",
                Puzzle = dataPuzzle,
                Solved = true,
            });

            await context.SaveChangesAsync();

            var repository = new EFRepository<MjrChess.Trainer.Data.Models.PuzzleHistory, PuzzleHistory>(
                context,
                mapper,
                NullLogger<EFRepository<MjrChess.Trainer.Data.Models.PuzzleHistory, PuzzleHistory>>.Instance);

            var history = await repository.Query(h => h.UserId == "user-2").SingleAsync();
            history.Solved = false;

            var updated = await repository.UpdateAsync(history);

            Assert.NotNull(updated);
            Assert.False(updated!.Solved);
            Assert.False(await context.PuzzleHistories.Where(h => h.UserId == "user-2").Select(h => h.Solved).SingleAsync());
        }

        private static PuzzleDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<PuzzleDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new PuzzleDbContext(options);
        }

        private static IMapper CreateMapper(PuzzleDbContext context)
        {
            var config = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddExpressionMapping();
                    cfg.AddProfile(new AutoMapperProfile(context));
                },
                NullLoggerFactory.Instance);

            return config.CreateMapper();
        }

        private static MjrChess.Trainer.Data.Models.TacticsPuzzle CreateDataPuzzle(int associatedPlayerId)
        {
            return new MjrChess.Trainer.Data.Models.TacticsPuzzle("rnbqk1nr/pppp1ppp/8/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR b KQkq - 3 3")
            {
                SetupMovedFrom = "b8",
                SetupMovedTo = "c6",
                MovedFrom = "f3",
                MovedTo = "f7",
                AssociatedPlayerId = associatedPlayerId,
                WhitePlayerName = "white",
                BlackPlayerName = "black",
            };
        }
    }
}

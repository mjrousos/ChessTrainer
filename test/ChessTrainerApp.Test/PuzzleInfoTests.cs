using Bunit;
using MjrChess.Engine;
using MjrChess.Trainer.Components;
using MjrChess.Trainer.Models;
using Xunit;

namespace ChessTrainerApp.Test
{
    public class PuzzleInfoTests : BunitContext
    {
        public PuzzleInfoTests()
        {
            JSInterop.SetupVoid("attachMDC");
        }

        [Fact]
        public void Description_WhenPuzzleHasSiteAndDate_RendersExpectedDescription()
        {
            var puzzle = CreatePuzzle();
            puzzle.Site = "lichess.org";
            puzzle.GameDate = new System.DateTimeOffset(2026, 1, 2, 0, 0, 0, System.TimeSpan.Zero);

            var cut = Render<PuzzleInfo>(parameters => parameters
                .Add(p => p.Puzzle, puzzle)
                .Add(p => p.PuzzleState, PuzzleState.Ongoing)
                .Add(p => p.PuzzleEngine, new ChessEngine()));

            Assert.Contains("lichess.org, 2026-01-02", cut.Markup);
        }

        [Fact]
        public void ActionButtons_WhenPuzzleIsOngoing_ShowsRevealAndNextOnly()
        {
            var cut = Render<PuzzleInfo>(parameters => parameters
                .Add(p => p.Puzzle, CreatePuzzle())
                .Add(p => p.PuzzleState, PuzzleState.Ongoing)
                .Add(p => p.PuzzleEngine, new ChessEngine()));

            Assert.Contains("help_outline", cut.Markup);
            Assert.Contains("skip_next", cut.Markup);
            Assert.DoesNotContain("replay", cut.Markup);
        }

        [Fact]
        public void StatusMessage_WhenPuzzleIsSolved_RendersCorrectMessage()
        {
            var cut = Render<PuzzleInfo>(parameters => parameters
                .Add(p => p.Puzzle, CreatePuzzle())
                .Add(p => p.PuzzleState, PuzzleState.Solved)
                .Add(p => p.PuzzleEngine, new ChessEngine()));

            Assert.Contains("Correct", cut.Markup);
        }

        private static TacticsPuzzle CreatePuzzle()
        {
            return new TacticsPuzzle(
                "rnbqk1nr/pppp1ppp/8/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR b KQkq - 3 3",
                "b8",
                "c6",
                null,
                "f3",
                "f7",
                null,
                null,
                null,
                null)
            {
                WhitePlayerName = "white",
                BlackPlayerName = "black",
            };
        }
    }
}

using System;
using MjrChess.Trainer.Models;
using Xunit;

namespace ChessTrainer.Common.Test
{
    public class TacticsPuzzleTests
    {
        [Fact]
        public void Constructor_WhenPositionIsEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TacticsPuzzle(
                string.Empty,
                "b8",
                "c6",
                null,
                "f3",
                "f7",
                null,
                null,
                null,
                null));
        }

        [Fact]
        public void WhiteToMove_WhenFenIndicatesBlackToMove_ReturnsTrue()
        {
            var puzzle = CreatePuzzle("rnbqk1nr/pppp1ppp/8/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR b KQkq - 3 3");

            Assert.True(puzzle.WhiteToMove);
        }

        [Fact]
        public void Constructor_WhenIncorrectMoveNotSpecified_LeavesIncorrectMoveNull()
        {
            var puzzle = CreatePuzzle("rnbqk1nr/pppp1ppp/8/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR b KQkq - 3 3");

            Assert.Null(puzzle.IncorrectMove);
        }

        private static TacticsPuzzle CreatePuzzle(string fen)
        {
            return new TacticsPuzzle(
                fen,
                "b8",
                "c6",
                null,
                "f3",
                "f7",
                null,
                null,
                null,
                null);
        }
    }
}

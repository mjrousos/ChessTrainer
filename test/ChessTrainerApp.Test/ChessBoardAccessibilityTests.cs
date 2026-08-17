using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MjrChess.Engine;
using MjrChess.Trainer.Components;
using Xunit;

namespace ChessTrainerApp.Test
{
    public class ChessBoardAccessibilityTests : BunitContext
    {
        public ChessBoardAccessibilityTests()
        {
            // Allow all JSInterop calls (addBoardKeyHandler, focusBoardCell, etc.)
            // without explicit setup — we're testing markup, not JS behaviour.
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void ChessBoard_RendersAccessibleGrid()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            var grid = cut.Find("[role='grid']");

            Assert.NotNull(grid);
            Assert.Equal("Chess board", grid.GetAttribute("aria-label"));
        }

        [Fact]
        public void ChessBoard_GridContains64Cells()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            var cells = cut.FindAll("[role='gridcell']");

            Assert.Equal(64, cells.Count);
        }

        [Fact]
        public void ChessBoard_CellsHaveAriaLabels()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            var cells = cut.FindAll("[role='gridcell']");

            foreach (var cell in cells)
            {
                var label = cell.GetAttribute("aria-label");
                Assert.False(string.IsNullOrWhiteSpace(label), "Every gridcell must have a non-empty aria-label");
            }
        }

        [Fact]
        public void ChessBoard_E1CellDescribesWhiteKing()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            // e1 = file 4, rank 0 — white king at e1 (its starting position)
            var e1 = cut.Find("[data-file='4'][data-rank='0']");

            Assert.Contains("king", e1.GetAttribute("aria-label"), System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("white", e1.GetAttribute("aria-label"), System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ChessBoard_EmptySquareLabelContainsOnlyCoordinate()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            // e4 is empty in the starting position (file 4, rank 3)
            var e4 = cut.Find("[data-file='4'][data-rank='3']");

            Assert.Equal("e4", e4.GetAttribute("aria-label"));
        }

        [Fact]
        public void ChessBoard_ExactlyOneCellHasTabindex0()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            var focusableCells = cut.FindAll("[role='gridcell'][tabindex='0']");

            Assert.Single(focusableCells);
        }

        [Fact]
        public void ChessBoard_HasAriaLiveRegion()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            var liveRegion = cut.Find("[aria-live='polite']");

            Assert.NotNull(liveRegion);
            Assert.Equal("status", liveRegion.GetAttribute("role"));
        }

        [Fact]
        public void ChessBoard_BoardImageIsDecorativeWithEmptyAlt()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            // The board background image should have alt="" (decorative).
            var boardImg = cut.Find("img.board");

            Assert.Equal(string.Empty, boardImg.GetAttribute("alt"));
        }

        [Fact]
        public void ChessBoard_PieceImagesAreDecorativeWithEmptyAlt()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            // All piece images should have alt="" — pieces are described by grid-cell labels.
            var pieceImgs = cut.FindAll("img.piece");

            Assert.NotEmpty(pieceImgs);
            foreach (var img in pieceImgs)
            {
                Assert.Equal(string.Empty, img.GetAttribute("alt"));
            }
        }

        [Fact]
        public void ChessBoard_GridHasEightRows()
        {
            var cut = Render<ChessBoard>(p => p.Add(
                x => x.Engine, new ChessEngine()));

            var rows = cut.FindAll("[role='row']");

            Assert.Equal(8, rows.Count);
        }
    }
}

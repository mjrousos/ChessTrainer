using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MjrChess.Engine;
using MjrChess.Engine.Models;
using MjrChess.Engine.Utilities;

namespace MjrChess.Trainer.Components
{
    /// <summary>
    /// Representation of a chess game.
    /// </summary>
    public class ChessBoardBase : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected ChessEngine Engine { get; set; }

        protected ChessGame Game => Engine?.Game;

        protected IEnumerable<Move> LegalMovesForSelectedPiece { get; set; } = Enumerable.Empty<Move>();

        private ChessPiece selectedPiece;

        /// <summary>
        /// Gets or sets the piece the user currently has selected.
        /// </summary>
        public ChessPiece SelectedPiece
        {
            get
            {
                return selectedPiece;
            }

            set
            {
                selectedPiece = value;
                LegalMovesForSelectedPiece = Engine.GetLegalMoves(selectedPiece);
            }
        }

        protected const string ElementName = "ChessBoard";

        // Tracks whether the component is rendered so that we know whether
        // to call StateHasChanged or not.
        private bool rendered = false;

        public ChessBoardBase()
        {
            Engine = new ChessEngine();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            rendered = true;
        }

        public async void HandleMouseDown(MouseEventArgs args)
        {
            if (SelectedPiece == null)
            {
                (var file, var rank) = await GetMousePositionAsync(args);
                SelectPiece(file, rank);
                Render();
            }
        }

        public async void HandleMouseUp(MouseEventArgs args)
        {
            if (SelectedPiece == null)
            {
                // If no piece is selected, do nothing
                return;
            }
            else
            {
                (var file, var rank) = await GetMousePositionAsync(args);
                if (SelectedPiece.File == file && SelectedPiece.Rank == rank)
                {
                    // If the mouse button is released on the same square the
                    // piece was selected from, do nothing. Keep the piece selected since
                    // this could be the initial click to select it.
                    return;
                }
                else
                {
                    PlacePiece(file, rank);
                    Render();
                }
            }
        }

        /// <summary>
        /// Attempts to select a game piece.
        /// </summary>
        /// <param name="file">The file of the piece to be selected.</param>
        /// <param name="rank">The rank of the piece to be selected.</param>
        /// <returns>True if a piece was successfully selected, false otherwise. Note that this does not guarantee the selected piece has any legal moves.</returns>
        public bool SelectPiece(int file, int rank)
        {
            var piece = Game.GetPiece(file, rank);
            if (piece == null || ChessFormatter.IsPieceWhite(piece.PieceType) != Game.WhiteToMove)
            {
                return false;
            }
            else
            {
                SelectedPiece = piece;
                return true;
            }
        }

        /// <summary>
        /// Attempts to place a selected piece. This unselects any selected piece.
        /// </summary>
        /// <param name="file">The file to place the selected piece on.</param>
        /// <param name="rank">The rank to place the selected piece on.</param>
        /// <returns>True if the selected piece was successully and legally placed on the indicated rank and file. False if the move is illegal or if no piece is selected.</returns>
        private bool PlacePiece(int file, int rank)
        {
            var move = LegalMovesForSelectedPiece.SingleOrDefault(m => m.FinalFile == file && m.FinalRank == rank);
            SelectedPiece = null;
            if (move != null)
            {
                // If the piece is placed in a legal move location,
                // move the piece.
                Game.Move(move);

                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<(int, int)> GetMousePositionAsync(MouseEventArgs args)
        {
            var boardDimensions = await JSRuntime.InvokeAsync<Rectangle>("getBoundingRectangle", new object[] { ElementName });

            // Account for the rare case where the user clicks on the final pixel of the board
            if (args.ClientX >= boardDimensions.Right)
            {
                args.ClientX--;
            }

            if (args.ClientY >= boardDimensions.Bottom)
            {
                args.ClientY--;
            }

            var file = ((int)args.ClientX - boardDimensions.X) * Game.BoardSize / boardDimensions.Width;
            var rank = (Game.BoardSize - 1) - (((int)args.ClientY - boardDimensions.Y) * Game.BoardSize / boardDimensions.Height);

            return (file, rank);
        }

        /// <summary>
        /// Tells Blazor to re-render the component
        /// </summary>
        private void Render()
        {
            if (rendered)
            {
                StateHasChanged();
            }
        }
    }
}

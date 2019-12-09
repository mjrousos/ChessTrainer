using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MjrChess.Engine.Models;

namespace MjrChess.Trainer.Components
{
    /// <summary>
    /// Representation of a chess game.
    /// </summary>
    public class ChessBoardBase : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        public ChessGame Game { get; set; }

        protected const string ElementName = "ChessBoard";

        // Tracks whether the component is rendered so that we know whether
        // to call StateHasChanged or not.
        private bool rendered = false;

        public ChessBoardBase()
        {
            Game = new ChessGame();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            rendered = true;
        }

        public async void HandleMouseDown(MouseEventArgs args)
        {
            if (Game.SelectedPiece == null)
            {
                (var file, var rank) = await GetMousePositionAsync(args);
                Game.SelectPiece(file, rank);
                Render();
            }
        }

        public async void HandleMouseUp(MouseEventArgs args)
        {
            if (Game.SelectedPiece == null)
            {
                // If no piece is selected, do nothing
                return;
            }
            else
            {
                (var file, var rank) = await GetMousePositionAsync(args);
                if (Game.SelectedPiece.File == file && Game.SelectedPiece.Rank == rank)
                {
                    // If the mouse button is released on the same square the
                    // piece was selected from, do nothing. Keep the piece selected since
                    // this could be the initial click to select it.
                    return;
                }
                else
                {
                    Game.PlacePiece(file, rank);
                    Render();
                }
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

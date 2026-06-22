using System;
using System.Collections.Generic;
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
    public class ChessBoardBase : ComponentBase, IDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter]
        public ChessEngine Engine { get; set; } = new ChessEngine();

        [Parameter]
        public bool UserMovableWhitePieces { get; set; } = true;

        [Parameter]
        public bool UserMovableBlackPieces { get; set; } = true;

        protected ChessGame Game => Engine.Game;

        protected Move[] LegalMovesForSelectedPiece { get; set; } = new Move[0];

        protected Move? LastMove => Game.Moves.LastOrDefault();

        /// <summary>Gets the current announcement text for the aria-live region.</summary>
        protected string? LastMoveAnnouncement { get; private set; }

        /// <summary>Gets or sets the file of the keyboard-focused board square (0 = a-file).</summary>
        protected int FocusedFile { get; set; } = 0;

        /// <summary>Gets or sets the rank of the keyboard-focused board square (0 = rank 1).</summary>
        protected int FocusedRank { get; set; } = 0;

        private ChessPiece? _selectedPiece;

        /// <summary>
        /// Gets or sets the piece the user currently has selected.
        /// </summary>
        public ChessPiece? SelectedPiece
        {
            get
            {
                return _selectedPiece;
            }

            set
            {
                _selectedPiece = value;

                // Storing an enumerable in state used by Blazor was causing the enumerable
                // to be evaluated multiple times. Therefore, store as an array to make sure
                // that the evaluation is only done once.
                LegalMovesForSelectedPiece = _selectedPiece == null ? new Move[0] : Engine.GetLegalMoves(_selectedPiece.Position).ToArray();
            }
        }

        protected const string AccessibleGridId = "ChessBoardGrid";

        // Tracks whether the component is rendered so that we know whether
        // to call StateHasChanged or not.
        private bool _rendered = false;

        public ChessBoardBase()
        {
            Engine = new ChessEngine();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // Subscribe to the game's move event so that we can announce opponent moves.
            Game.OnMove -= HandleGameMove;
            Game.OnMove += HandleGameMove;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            _rendered = true;

            if (firstRender)
            {
                // Attach a synchronous JS listener so that arrow-key and Space default
                // browser actions (page scroll) are suppressed while the board has focus,
                // without blocking Tab (which is intentionally excluded).
                await JSRuntime.InvokeVoidAsync("addBoardKeyHandler", AccessibleGridId);
            }
        }

        /// <summary>
        /// Handles a click or keyboard activation on a specific board square.
        /// </summary>
        /// <param name="file">The file of the activated square.</param>
        /// <param name="rank">The rank of the activated square.</param>
        public void HandleCellClick(int file, int rank)
        {
            FocusedFile = file;
            FocusedRank = rank;

            if (SelectedPiece != null)
            {
                var moved = PlacePiece(file, rank);
                if (!moved)
                {
                    // Not a legal destination — try selecting a different piece on that square.
                    SelectPiece(file, rank);
                }
            }
            else
            {
                SelectPiece(file, rank);
            }

            Render();
        }

        /// <summary>
        /// Handles keyboard events on the accessible board grid.
        /// Arrow keys move focus; Enter/Space activate the focused square; Escape deselects.
        /// </summary>
        /// <param name="args">Keyboard event arguments.</param>
        public async Task HandleKeyDown(KeyboardEventArgs args)
        {
            switch (args.Key)
            {
                case "ArrowLeft":
                    FocusedFile = Math.Max(0, FocusedFile - 1);
                    break;
                case "ArrowRight":
                    FocusedFile = Math.Min(Game.BoardSize - 1, FocusedFile + 1);
                    break;
                case "ArrowUp":
                    FocusedRank = Math.Min(Game.BoardSize - 1, FocusedRank + 1);
                    break;
                case "ArrowDown":
                    FocusedRank = Math.Max(0, FocusedRank - 1);
                    break;
                case "Enter":
                case " ":
                    HandleCellClick(FocusedFile, FocusedRank);
                    return;
                case "Escape":
                    SelectedPiece = null;
                    Render();
                    return;
                default:
                    return;
            }

            Render();

            // Move DOM focus to the newly focused cell after re-render.
            await JSRuntime.InvokeVoidAsync("focusBoardCell", AccessibleGridId, FocusedFile, FocusedRank);
        }

        /// <summary>
        /// Attempts to select a game piece.
        /// </summary>
        /// <param name="file">The file of the piece to be selected.</param>
        /// <param name="rank">The rank of the piece to be selected.</param>
        /// <returns>True if a piece was successfully selected, false otherwise. Note that this does not guarantee the selected piece has any legal moves.</returns>
        public bool SelectPiece(int file, int rank)
        {
            // Don't select pieces if the game is finished
            if (Game?.Result != GameResult.Ongoing)
            {
                return false;
            }

            // Don't select pieces if the user isn't allowed to move the active color's pieces
            if ((Game.WhiteToMove && !UserMovableWhitePieces) ||
                (!Game.WhiteToMove && !UserMovableBlackPieces))
            {
                return false;
            }

            var piece = Game.GetPiece(file, rank);

            // Don't select pieces if the clicked square doesn't contain a piece or contains a piece for the wrong player
            if (piece == null || ChessFormatter.IsPieceWhite(piece.PieceType) != Game.WhiteToMove)
            {
                return false;
            }

            SelectedPiece = piece;
            return true;
        }

        /// <summary>
        /// Returns the ARIA label for a board square, describing its position and any piece it contains.
        /// </summary>
        /// <param name="file">File index (0 = a-file).</param>
        /// <param name="rank">Rank index (0 = rank 1).</param>
        /// <returns>A human-readable description suitable for an aria-label attribute.</returns>
        protected string GetSquareAriaLabel(int file, int rank)
        {
            var squareName = $"{ChessFormatter.FileToString(file)}{ChessFormatter.RankToString(rank)}";
            var piece = Game.GetPiece(file, rank);

            if (piece == null)
            {
                return squareName;
            }

            var color = ChessFormatter.IsPieceWhite(piece.PieceType) ? "white" : "black";
            var pieceName = GetPieceName(piece.PieceType);
            return $"{squareName}, {color} {pieceName}";
        }

        /// <summary>
        /// Returns extra CSS classes for a board cell reflecting selection and legal-move state.
        /// </summary>
        /// <param name="file">File index.</param>
        /// <param name="rank">Rank index.</param>
        /// <returns>A space-separated CSS class string (may be empty).</returns>
        protected string GetCellStateClass(int file, int rank)
        {
            var classes = new List<string>();

            if (SelectedPiece?.Position.File == file && SelectedPiece?.Position.Rank == rank)
            {
                classes.Add("boardCell--selected");
            }

            if (LegalMovesForSelectedPiece.Any(m => m.FinalPosition.File == file && m.FinalPosition.Rank == rank))
            {
                classes.Add("boardCell--legal");
            }

            return string.Join(" ", classes);
        }

        /// <summary>
        /// Attempts to place a selected piece. This unselects any selected piece.
        /// </summary>
        /// <param name="file">The file to place the selected piece on.</param>
        /// <param name="rank">The rank to place the selected piece on.</param>
        /// <returns>True if the selected piece was successully and legally placed on the indicated rank and file. False if the move is illegal or if no piece is selected.</returns>
        private bool PlacePiece(int file, int rank)
        {
            var move = LegalMovesForSelectedPiece.SingleOrDefault(m => m.FinalPosition.File == file && m.FinalPosition.Rank == rank);
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

        /// <summary>Called by the game's OnMove event to update the aria-live announcement.</summary>
        private void HandleGameMove(ChessGame game, Move move)
        {
            var color = ChessFormatter.IsPieceWhite(move.PieceMoved) ? "White" : "Black";
            LastMoveAnnouncement = $"{color} plays {ChessFormatter.MoveToString(move)}";
            Render();
        }

        /// <summary>
        /// Tells Blazor to re-render the component.
        /// </summary>
        private void Render()
        {
            if (_rendered)
            {
                StateHasChanged();
            }
        }

        private static string GetPieceName(ChessPieces piece) => piece switch
        {
            ChessPieces.WhiteKing or ChessPieces.BlackKing => "king",
            ChessPieces.WhiteQueen or ChessPieces.BlackQueen => "queen",
            ChessPieces.WhiteRook or ChessPieces.BlackRook => "rook",
            ChessPieces.WhiteBishop or ChessPieces.BlackBishop => "bishop",
            ChessPieces.WhiteKnight or ChessPieces.BlackKnight => "knight",
            ChessPieces.WhitePawn or ChessPieces.BlackPawn => "pawn",
            _ => string.Empty,
        };

        /// <inheritdoc />
        public void Dispose()
        {
            // Unsubscribe so the component doesn't receive move notifications after disposal.
            Game.OnMove -= HandleGameMove;
        }
    }
}

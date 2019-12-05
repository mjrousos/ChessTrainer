using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ChessTrainerApp.Components
{
    /// <summary>
    /// Representation of a chess game.
    /// </summary>
    public class ChessBoardBase : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private const int BoardSize = 8;
        protected const string ElementName = "ChessBoard";
        private const string InitialGameFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Stores which piece (if any) is on each board space
        private ChessPiece[][] boardState;

        /// <summary>
        /// Gets or sets moves since initial position.
        /// </summary>
        public IList<Move> Moves { get; set; }

        public IEnumerable<Move> LegalMovesForSelectedPiece { get; set; }

        /// <summary>
        /// Gets pieces currently on the board.
        /// </summary>
        public IEnumerable<ChessPiece> Pieces
        {
            get
            {
                for (var file = 0; file < BoardSize; file++)
                {
                    for (var rank = 0; rank < BoardSize; rank++)
                    {
                        if (boardState[file][rank] != null)
                        {
                            yield return boardState[file][rank];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets name of the player with the white pieces (if known).
        /// </summary>
        public string WhitePlayer { get; set; }

        /// <summary>
        /// Gets or sets name of the player with the black pieces (if known).
        /// </summary>
        public string BlackPlayer { get; set; }

        /// <summary>
        /// Gets or sets the game's result.
        /// </summary>
        public GameResult Result { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it's the white player's turn to move.
        /// </summary>
        public bool WhiteToMove { get; set; }

        /// <summary>
        /// Gets or sets castling options available to the black pieces.
        /// </summary>
        public CastlingOptions BlackCastlingOptions { get; set; }

        /// <summary>
        /// Gets or sets castling options available to the white pieces.
        /// </summary>
        public CastlingOptions WhiteCastlingOptions { get; set; }

        /// <summary>
        /// Gets or sets the square an en passant capture can be made to (if any).
        /// </summary>
        public (int, int)? EnPassantTarget { get; set; }

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
                LegalMovesForSelectedPiece = GetLegalMoves(selectedPiece);
            }
        }

        /// <summary>
        /// Gets or sets the number of half moves since a pawn was moved or a piece was captured.
        /// </summary>
        public int HalfMoveClock { get; set; }

        /// <summary>
        /// Gets or sets the number of full moves made (beginning with 1 are the start of the game).
        /// </summary>
        public int MoveCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessBoardBase"/> class, setup for a new standard chess game.
        /// </summary>
        public ChessBoardBase()
        {
            LoadFEN(InitialGameFEN);
        }

        /// <summary>
        /// Retrieves potential legal moves for a given piece.
        /// </summary>
        /// <param name="pieceToMove">The piece to move.</param>
        /// <returns>All possible legal moves for the piece.</returns>
        public IEnumerable<Move> GetLegalMoves(ChessPiece pieceToMove)
        {
            if (pieceToMove != null)
            {
                // TODO - TEMPORARILY RETURN ALL SQUARES AS LEGAL. THIS MUST BE FIXED.
                for (var i = 0; i < BoardSize; i++)
                {
                    for (var j = 0; j < BoardSize; j++)
                    {
                        yield return new Move
                        {
                            PieceMoved = pieceToMove.PieceType,
                            OriginalFile = pieceToMove.File,
                            OriginalRank = pieceToMove.Rank,
                            FinalFile = i,
                            FinalRank = j
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Clears the board and all game state.
        /// </summary>
        public void ClearGameState()
        {
            Moves = new List<Move>();
            WhitePlayer =
                BlackPlayer = null;
            WhiteToMove = true;
            WhiteCastlingOptions =
                BlackCastlingOptions = CastlingOptions.KingSide | CastlingOptions.QueenSide;
            EnPassantTarget = null;
            SelectedPiece = null;
            Result = GameResult.Ongoing;
            HalfMoveClock = 0;
            MoveCount = 1;
            boardState = new ChessPiece[BoardSize][];
            for (var i = 0; i < boardState.GetLength(0); i++)
            {
                boardState[i] = new ChessPiece[BoardSize];
            }
        }

        /// <summary>
        /// Initialize the board to a position given in FEN format. https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation.
        /// </summary>
        /// <param name="fen">The FEN-formatted game state to load.</param>
        public void LoadFEN(string fen)
        {
            ClearGameState();
            var fenComponents = fen?.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            // Parse piece positions
            if (fenComponents.Length > 0)
            {
                var rank = 7;
                var file = 0;
                foreach (var piece in fenComponents[0])
                {
                    switch (piece)
                    {
                        case '/':
                            rank--;
                            file = 0;
                            break;
                        case 'K':
                        case 'k':
                        case 'Q':
                        case 'q':
                        case 'R':
                        case 'r':
                        case 'B':
                        case 'b':
                        case 'N':
                        case 'n':
                        case 'P':
                        case 'p':
                            boardState[file][rank] = new ChessPiece(
                                ChessFormatter.PieceFromString($"{piece}"),
                                file,
                                rank);
                            file++;
                            break;
                        default:
                            if (int.TryParse($"{piece}", out var spaceCount))
                            {
                                file += spaceCount;
                            }

                            break;
                    }
                }
            }

            // Parse active color
            if (fenComponents.Length > 1)
            {
                WhiteToMove = fenComponents[1].Equals("w", StringComparison.InvariantCultureIgnoreCase);
            }

            // Parse castling options
            if (fenComponents.Length > 2)
            {
                WhiteCastlingOptions = CastlingOptions.None;
                BlackCastlingOptions = CastlingOptions.None;

                var castlingOptions = fenComponents[2];
                if (castlingOptions.Contains('K', StringComparison.InvariantCulture))
                {
                    WhiteCastlingOptions |= CastlingOptions.KingSide;
                }

                if (castlingOptions.Contains('Q', StringComparison.InvariantCulture))
                {
                    WhiteCastlingOptions |= CastlingOptions.QueenSide;
                }

                if (castlingOptions.Contains('k', StringComparison.InvariantCulture))
                {
                    BlackCastlingOptions |= CastlingOptions.KingSide;
                }

                if (castlingOptions.Contains('q', StringComparison.InvariantCulture))
                {
                    BlackCastlingOptions |= CastlingOptions.QueenSide;
                }
            }

            // Parse en passant target
            if (fenComponents.Length > 3 && fenComponents[3].Length == 2)
            {
                EnPassantTarget = (
                    ChessFormatter.FileFromString($"{fenComponents[3][0]}"),
                    ChessFormatter.RankFromString($"{fenComponents[3][1]}"));
            }

            // Parse half move clock
            if (fenComponents.Length > 4)
            {
                HalfMoveClock = int.Parse(fenComponents[4], CultureInfo.InvariantCulture);
            }

            // Parse move count
            if (fenComponents.Length > 5)
            {
                MoveCount = int.Parse(fenComponents[5], CultureInfo.InvariantCulture);
            }

            // TODO : Set Result
        }

        /// <summary>
        /// Initialize the board to a position given in PGN format. https://en.wikipedia.org/wiki/Portable_Game_Notation.
        /// </summary>
        /// <param name="pgn">The PGN-formatted game state to load.</param>
        public void LoadPGN(string pgn)
        {
            ClearGameState();
            throw new NotImplementedException("NYI");
        }

        /// <summary>
        /// Make a move.
        /// </summary>
        /// <param name="move">The move to apply to the game state.</param>
        public void Move(Move move)
        {
            // Check that move is legal

            // Add to moves list
            Moves.Add(move);

            // Adjust piece positions

            // Make additional board adjustments in cases of castling, en passant, or promotion

            // Increment or reset half move clock

            // Increment move count if black moved

            // Update en passant target, if necessary

            // Update castling options, if necessary

            // Update active color
        }

        /// <summary>
        /// Get the game state in FEN notation. https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation.
        /// </summary>
        /// <returns>FEN notation description of the game state.</returns>
        public string GetFEN()
        {
            throw new NotImplementedException(this.WhitePlayer);
        }

        /// <summary>
        /// Get the game state in PGN notation. https://en.wikipedia.org/wiki/Portable_Game_Notation.
        /// </summary>
        /// <returns>PGN notation description of the game state.</returns>
        public string GetPGN()
        {
            throw new NotImplementedException(this.WhitePlayer);
        }

        public async void HandleMouseDown(MouseEventArgs args)
        {
            if (SelectedPiece == null)
            {
                (var file, var rank) = await GetMousePositionAsync(args);
                SelectPiece(file, rank);
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
                PlacePiece(file, rank);
            }
        }

        public void HandleMouseMove(MouseEventArgs args)
        {
            if (SelectedPiece != null)
            {
                // Move selected piece image
                // TODO
            }
        }

        private void SelectPiece(int file, int rank)
        {
            var piece = boardState[file][rank];
            if (piece == null || ChessFormatter.IsPieceWhite(piece.PieceType) != WhiteToMove)
            {
                return;
            }
            else
            {
                SelectedPiece = piece;
            }
        }

        private void PlacePiece(int file, int rank)
        {
            if (selectedPiece.File == file && selectedPiece.Rank == rank)
            {
                // If the mouse button is released on the same square the
                // piece was selected from, do nothing. Keep the piece selected since
                // this could be the initial click to select it.
                return;
            }
            else
            {
                var move = LegalMovesForSelectedPiece.SingleOrDefault(m => m.FinalFile == file && m.FinalRank == rank);
                SelectedPiece = null;
                if (move != null)
                {
                    // If the piece is placed in a legal move location,
                    // move the piece.
                    Move(move);
                }
                else
                {
                    // If the piece is not placed in a legal move location,
                    // try selecting the piece in that location instead.
                    SelectPiece(file, rank);
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

            var file = ((int)args.ClientX - boardDimensions.X) * BoardSize / boardDimensions.Width;
            var rank = (BoardSize - 1) - (((int)args.ClientY - boardDimensions.Y) * BoardSize / boardDimensions.Height);

            return (file, rank);
        }

        /// <summary>
        /// Returns a string representation of the game (in FEN notation).
        /// </summary>
        /// <returns>FEN notation description of the game state.</returns>
        public override string ToString()
        {
            return GetFEN();
        }
    }
}

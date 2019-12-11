﻿using System;
using System.Collections.Generic;
using System.Globalization;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    public class ChessGame
    {
        public const int DefaultBoardSize = 8;
        private const string InitialGameFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Stores which piece (if any) is on each board space
        private ChessPiece[][] boardState;

        public int BoardSize { get; set; } = DefaultBoardSize;

        /// <summary>
        /// Gets or sets moves since initial position.
        /// </summary>
        public IList<Move> Moves { get; set; }

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
        public BoardPosition EnPassantTarget { get; set; }

        /// <summary>
        /// Gets or sets the number of half moves since a pawn was moved or a piece was captured.
        /// </summary>
        public int HalfMoveClock { get; set; }

        /// <summary>
        /// Gets or sets the number of full moves made (beginning with 1 are the start of the game).
        /// </summary>
        public int MoveCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a player's king is in check.
        /// </summary>
        public bool Check { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGame"/> class, setup for a new standard chess game.
        /// </summary>
        public ChessGame()
        {
            LoadFEN(InitialGameFEN);
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
            Result = GameResult.Ongoing;
            HalfMoveClock = 0;
            MoveCount = 1;
            Check = false;
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
            var fenComponents = fen?.Split(new char[] { ' ' }, 6, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

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
                                new BoardPosition(file, rank));
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
                if (castlingOptions.Contains("K"))
                {
                    WhiteCastlingOptions |= CastlingOptions.KingSide;
                }

                if (castlingOptions.Contains("Q"))
                {
                    WhiteCastlingOptions |= CastlingOptions.QueenSide;
                }

                if (castlingOptions.Contains("k"))
                {
                    BlackCastlingOptions |= CastlingOptions.KingSide;
                }

                if (castlingOptions.Contains("q"))
                {
                    BlackCastlingOptions |= CastlingOptions.QueenSide;
                }
            }

            // Parse en passant target
            if (fenComponents.Length > 3 && fenComponents[3].Length == 2)
            {
                EnPassantTarget = new BoardPosition(
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
        /// Gets the chess piece at a particular board position.
        /// </summary>
        /// <param name="position">The position to retrieve a piece from.</param>
        /// <returns>The piece on the indicated square or null if no piece is there.</returns>
        public ChessPiece GetPiece(BoardPosition position) => GetPiece(position.File, position.Rank);

        /// <summary>
        /// Gets the chess piece at a particular board position.
        /// </summary>
        /// <param name="file">The file the piece is on.</param>
        /// <param name="rank">The rank the piece is on.</param>
        /// <returns>The piece on the indicated square or null if no piece is there.</returns>
        public ChessPiece GetPiece(int file, int rank)
        {
            if (file >= BoardSize || rank >= BoardSize || file < 0 || rank < 0)
            {
                return null;
            }

            return boardState[file][rank];
        }

        /// <summary>
        /// Make a move.
        /// </summary>
        /// <param name="move">The move to apply to the game state.</param>
        public void Move(Move move)
        {
            // Add to moves list
            Moves.Add(move);

            // Adjust piece positions
            boardState[move.OriginalPosition.File][move.OriginalPosition.Rank] = null;
            boardState[move.FinalPosition.File][move.FinalPosition.Rank] = new ChessPiece(move.PiecePromotedTo ?? move.PieceMoved, move.FinalPosition);

            // Make additional board adjustments in cases of castling, en passant, or promotion

            // Increment or reset half move clock

            // Update en passant target, if necessary

            // Update castling options, if necessary

            // Update active color
            WhiteToMove = !WhiteToMove;
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

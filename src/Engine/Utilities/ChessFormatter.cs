using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MjrChess.Engine.Models;

namespace MjrChess.Engine.Utilities
{
    /// <summary>
    /// Helper methods for translating chess concepts to or from strings.
    /// </summary>
    public static class ChessFormatter
    {
        public static ChessPieces[] WhitePieces => new[]
        {
            ChessPieces.WhiteKing,
            ChessPieces.WhiteQueen,
            ChessPieces.WhiteRook,
            ChessPieces.WhiteBishop,
            ChessPieces.WhiteKnight,
            ChessPieces.WhitePawn
        };

        public static ChessPieces[] StartingWhitePieces => new[]
        {
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhitePawn,
            ChessPieces.WhiteRook,
            ChessPieces.WhiteKnight,
            ChessPieces.WhiteBishop,
            ChessPieces.WhiteQueen,
            ChessPieces.WhiteKing,
            ChessPieces.WhiteBishop,
            ChessPieces.WhiteKnight,
            ChessPieces.WhiteRook
        };

        public static ChessPieces[] StartingBlackPieces => new[]
        {
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackPawn,
            ChessPieces.BlackRook,
            ChessPieces.BlackKnight,
            ChessPieces.BlackBishop,
            ChessPieces.BlackQueen,
            ChessPieces.BlackKing,
            ChessPieces.BlackBishop,
            ChessPieces.BlackKnight,
            ChessPieces.BlackRook
        };

        public static string RankToString(int rank) => $"{rank + 1}";

        public static int RankFromString(string rank) => int.Parse(rank, CultureInfo.InvariantCulture) - 1;

        public static string FileToString(int file) => $"{(char)(file + 0x61)}";

        public static int FileFromString(string file) => file[0] - 0x61;

        /// <summary>
        /// Converts a chess piece to an approximate numerical value.
        /// </summary>
        /// <param name="piece">The piece to be converted to a value.</param>
        /// <returns>The approximate number of pawns the piece is worth.</returns>
        public static int GetPieceValue(ChessPieces piece) =>
            piece switch
            {
                // TODO : This is fine for purposes of displaying advantage,
                //        but if this develops into a fuller chess engine, it
                //        will need a more sophisticated system and kings will need
                //        a value.
                ChessPieces.WhiteQueen => 9,
                ChessPieces.BlackQueen => -9,
                ChessPieces.WhiteRook => 5,
                ChessPieces.BlackRook => -5,
                ChessPieces.WhiteBishop => 3,
                ChessPieces.BlackBishop => -3,
                ChessPieces.WhiteKnight => 3,
                ChessPieces.BlackKnight => -3,
                ChessPieces.WhitePawn => 1,
                ChessPieces.BlackPawn => -1,
                _ => 0
            };

        public static string ResultToString(GameResult result) =>
            result switch
            {
                GameResult.BlackWins => "0-1",
                GameResult.WhiteWins => "1-0",
                GameResult.Draw => "1/2-1/2",
                _ => "*"
            };

        public static GameResult ResultFromString(string result) =>
            result switch
            {
                "0-1" => GameResult.BlackWins,
                "1-0" => GameResult.WhiteWins,
                "1/2-1/2" => GameResult.Draw,
                "*" => GameResult.Ongoing,
                _ => throw new ArgumentException("Invalid result string", nameof(result))
            };

        /// <summary>
        /// Converts a chess piece into a string representation.
        /// </summary>
        /// <param name="piece">The piece to convert to a string.</param>
        /// <param name="fenStyle">True to use lower-case characters for black pieces.</param>
        /// <param name="pForPawn">True to use 'P' for pawns, otherwise pawns convert to empty strings.</param>
        /// <returns>A string representation of the chess piece.</returns>
        public static string PieceToString(ChessPieces piece, bool fenStyle = false, bool pForPawn = false)
        {
            var ret = piece switch
            {
                ChessPieces.WhiteKing => "K",
                ChessPieces.BlackKing => "k",
                ChessPieces.WhiteQueen => "Q",
                ChessPieces.BlackQueen => "q",
                ChessPieces.WhiteRook => "R",
                ChessPieces.BlackRook => "r",
                ChessPieces.WhiteBishop => "B",
                ChessPieces.BlackBishop => "b",
                ChessPieces.WhiteKnight => "N",
                ChessPieces.BlackKnight => "n",
                ChessPieces.WhitePawn => pForPawn ? "P" : string.Empty,
                ChessPieces.BlackPawn => pForPawn ? "p" : string.Empty,
                _ => "\uFFFD",
            };

            if (!fenStyle)
            {
                ret = ret.ToUpperInvariant();
            }

            return ret;
        }

        /// <summary>
        /// Get a chess piece from a string descrption.
        /// </summary>
        /// <param name="piece">A string representing a chess piece.</param>
        /// <returns>A chess piece.</returns>
        /// <remarks>Capital letters are interpreted as white pieces and lower case letters as black pieces (as in FEN notation).</remarks>
        public static ChessPieces PieceFromString(string piece)
        {
            return piece switch
            {
                "K" => ChessPieces.WhiteKing,
                "k" => ChessPieces.BlackKing,
                "Q" => ChessPieces.WhiteQueen,
                "q" => ChessPieces.BlackQueen,
                "R" => ChessPieces.WhiteRook,
                "r" => ChessPieces.BlackRook,
                "B" => ChessPieces.WhiteBishop,
                "b" => ChessPieces.BlackBishop,
                "N" => ChessPieces.WhiteKnight,
                "n" => ChessPieces.BlackKnight,
                "P" => ChessPieces.WhitePawn,
                "p" => ChessPieces.BlackPawn,
                _ => throw new ArgumentException("Invalid piece identifier", nameof(piece))
            };
        }

        /// <summary>
        /// Format a chess move in standard algebraic notation. https://en.wikipedia.org/wiki/Algebraic_notation_(chess).
        /// </summary>
        /// <param name="move">The move to convert to a string.</param>
        /// <returns>A standard algebraic notation description of the move.</returns>
        public static string MoveToString(Move move)
        {
            var output = new StringBuilder();

            if (move.ShortCastle)
            {
                return "O-O";
            }

            if (move.LongCastle)
            {
                return "O-O-O";
            }

            // Begin with the piece type unless the piece was a pawn,
            // in which case the original file is used for captures and an
            // empty string for moves.
            var pieceMoved = PieceToString(move.PieceMoved);
            output.Append(
                string.IsNullOrEmpty(pieceMoved) ?
                (move.Capture ? FileToString(move.OriginalPosition.File) : string.Empty) :
                pieceMoved);

            if (move.AmbiguousOriginalFile)
            {
                output.Append(FileToString(move.OriginalPosition.File));
            }

            if (move.AmbiguousOriginalRank)
            {
                output.Append(RankToString(move.OriginalPosition.Rank));
            }

            if (move.Capture)
            {
                output.Append("x");
            }

            output.Append($"{move.FinalPosition}");

            if (move.PiecePromotedTo != null)
            {
                output.Append($"={PieceToString(move.PiecePromotedTo.Value)}");
            }

            if (move.Checkmates)
            {
                output.Append("#");
            }
            else if (move.Checks)
            {
                output.Append("+");
            }

            return output.ToString();
        }

        public static string MovesToString(IEnumerable<Move> moves, int firstMoveCount = 1)
        {
            var movesString = new StringBuilder();
            var lineBreak = 0;
            var moveCount = firstMoveCount;

            foreach (var move in moves)
            {
                var whiteMove = IsPieceWhite(move.PieceMoved);

                if (whiteMove)
                {
                    movesString.Append($"{moveCount}. {MoveToString(move)} ");
                }
                else
                {
                    if (movesString.Length == 0)
                    {
                        // In the rare case we start with black to move, use this notation
                        movesString.Append($"{moveCount}... ");
                    }

                    movesString.Append($"{MoveToString(move)} ");
                }

                if (movesString.Length - lineBreak > 74)
                {
                    // PGN lines should not be more than 80 characters. If line length is getting close
                    // to that, remove the trailing space and go to the next line.
                    movesString.Remove(movesString.Length - 1, 1);
                    movesString.AppendLine();
                    lineBreak = movesString.Length;
                }

                if (!whiteMove)
                {
                    moveCount++;
                }
            }

            return movesString.ToString().TrimEnd();
        }

        public static bool IsPieceWhite(ChessPieces piece) => WhitePieces.Contains(piece);
    }
}

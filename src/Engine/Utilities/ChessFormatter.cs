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

        public static string RankToString(int rank) => $"{rank + 1}";

        public static int RankFromString(string rank) => int.Parse(rank, CultureInfo.InvariantCulture) - 1;

        public static string FileToString(int file) => $"{(char)(file + 0x61)}";

        public static int FileFromString(string file) => file[0] - 0x61;

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
                _ => null,
            };

            if (!fenStyle)
            {
                ret = ret?.ToUpperInvariant();
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

        public static string MovesToString(IEnumerable<Move> moves, bool boldLastMove = false)
        {
            // TODO : Replace dummy data
            return "1.Nf3 Nf6 2.c4 g6 3.Nc3 Bg7 4.d4 O-O 5.Bf4 d5 6.Qb3 dxc4 7.Qxc4 c6 8.e4 Nbd7 9.Rd1 Nb6 10.Qc5 Bg4 11.Bg5 Na4 12.Qa3 Nxc3 13.bxc3 Nxe4 14.Bxe7 Qb6 15.Bc4 Nxc3 16.Bc5 Rfe8+ 17.Kf1 Be6 18.Bxb6 Bxc4+ 19.Kg1 Ne2+ 20.Kf1 Nxd4+ 21.Kg1 Ne2+ 22.Kf1 Nc3+ 23.Kg1 axb6 24.Qb4 Ra4 25.Qxb6 Nxd1 26.h3 Rxa2 27.Kh2 Nxf2 28.Re1 Rxe1 29.Qd8+ Bf8 30.Nxe1 Bd5 31.Nf3 Ne4 32.Qb8 b5 33.h4 h5 34.Ne5 Kg7 35.Kg1 Bc5+ 36.Kf1 Ng3+ 37.Ke1 Bb4+ 38.Kd1 Bb3+ 39.Kc1 Ne2+ 40.Kb1 Nc3+ 41.Kc1 <b>Rc2#</b>";
        }

        public static bool IsPieceWhite(ChessPieces piece) => WhitePieces.Contains(piece);
    }
}

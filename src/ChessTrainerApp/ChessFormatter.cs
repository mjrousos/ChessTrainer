using System;
using System.Globalization;
using System.Text;

namespace ChessTrainerApp
{
    /// <summary>
    /// Helper methods for translating chess concepts to or from strings.
    /// </summary>
    public static class ChessFormatter
    {
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
                (move.Capture ? FileToString(move.OriginalFile) : string.Empty) :
                pieceMoved);

            if (move.AmbiguousOriginalFile)
            {
                output.Append(FileToString(move.OriginalFile));
            }

            if (move.AmbiguousOriginalRank)
            {
                output.Append(RankToString(move.OriginalRank));
            }

            if (move.Capture)
            {
                output.Append("x");
            }

            output.Append($"{FileToString(move.FinalFile)}{RankToString(move.FinalRank)}");

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
    }
}

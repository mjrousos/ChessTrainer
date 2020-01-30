using System;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    /// <summary>
    /// A single move in a chess game.
    /// </summary>
    public class Move : IEquatable<Move>
    {
        // https://github.com/dotnet/csharplang/issues/2328
        public Move(ChessPieces pieceMoved, BoardPosition originalPosition, BoardPosition finalPosition, ChessPieces? piecePromotedTo = null)
        {
            PieceMoved = pieceMoved;
            OriginalPosition = originalPosition;
            FinalPosition = finalPosition;
            PiecePromotedTo = piecePromotedTo;
        }

        public BoardPosition OriginalPosition { get; set; }

        public BoardPosition FinalPosition { get; set; }

        public bool AmbiguousOriginalRank { get; set; }

        public bool AmbiguousOriginalFile { get; set; }

        public bool Capture { get; set; }

        public bool Checks { get; set; }

        public bool Checkmates { get; set; }

        public bool Stalemates { get; set; }

        public bool ShortCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalPosition.File == 4 &&
            FinalPosition.File == 6;

        public bool LongCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalPosition.File == 4 &&
            FinalPosition.File == 2;

        public ChessPieces PieceMoved { get; set; }

        public ChessPieces? PiecePromotedTo { get; set; }

        public bool Equals(Move? other) =>
            !(other is null) &&
            PieceMoved == other.PieceMoved &&
            OriginalPosition == other.OriginalPosition &&
            FinalPosition == other.FinalPosition &&
            PiecePromotedTo == other.PiecePromotedTo;

        public override bool Equals(object obj) => (obj is Move move) ? Equals(move) : false;

        public override int GetHashCode() => $"{PieceMoved}{OriginalPosition}{FinalPosition}{PiecePromotedTo}".GetHashCode();

        public static bool operator ==(Move? lhs, Move? rhs) => lhs?.Equals(rhs) ?? rhs is null;

        public static bool operator !=(Move? lhs, Move? rhs) => !(lhs == rhs);

        public override string ToString() => ChessFormatter.MoveToString(this);
    }
}

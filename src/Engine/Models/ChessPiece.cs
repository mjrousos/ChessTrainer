using System;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    public class ChessPiece : IEquatable<ChessPiece>
    {
        public ChessPiece(ChessPieces pieceType, BoardPosition position)
        {
            PieceType = pieceType;
            Position = position;
        }

        public ChessPieces PieceType { get; set; }

        public BoardPosition Position { get; set; }

        public bool Equals(ChessPiece? other) =>
            PieceType == other?.PieceType &&
            Position == other?.Position;

        public override bool Equals(object obj) => (obj is ChessPiece piece) ? Equals(piece) : false;

        public override int GetHashCode() => (Position.GetHashCode() * Enum.GetValues(typeof(ChessPieces)).Length) + PieceType.GetHashCode();

        public override string ToString() =>
            string.Join(" ",
                ChessFormatter.PieceToString(PieceType, pForPawn: true),
                Position?.ToString());

        public static bool operator ==(ChessPiece? lhs, ChessPiece? rhs)
        {
            // Check for null on left side.
            if (lhs is null)
            {
                if (rhs is null)
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }

            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ChessPiece? lhs, ChessPiece? rhs)
        {
            return !(lhs == rhs);
        }
    }
}

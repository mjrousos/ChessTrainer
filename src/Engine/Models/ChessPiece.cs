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

        public bool Equals(ChessPiece other) =>
            PieceType == other.PieceType &&
            Position == other.Position;

        public override bool Equals(object obj) => (obj is ChessPiece piece) ? Equals(piece) : false;

        public override int GetHashCode() => (Position.GetHashCode() * Enum.GetValues(typeof(ChessPieces)).Length) + PieceType.GetHashCode();

        public override string ToString() =>
            string.Join(" ",
                ChessFormatter.PieceToString(PieceType, pForPawn: true),
                Position?.ToString());
    }
}

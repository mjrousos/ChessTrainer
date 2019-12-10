using System;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    public class ChessPiece : IEquatable<ChessPiece>
    {
        public ChessPiece(ChessPieces pieceType, int file, int rank)
        {
            PieceType = pieceType;
            File = file;
            Rank = rank;
        }

        public ChessPieces PieceType { get; set; }

        public int? File { get; set; }

        public int? Rank { get; set; }

        public bool Equals(ChessPiece other) =>
            PieceType == other.PieceType &&
            File == other.File &&
            Rank == other.Rank;

        public override bool Equals(object obj) => (obj is ChessPiece piece) ? Equals(piece) : false;

        // This is probably 'good enough' unless/until collisions are found to be a perf problem.
        // It should always agree with equality for valid pieces, the only question is whether the hashes are uniform enough.
        public override int GetHashCode() => $"{File.ToString()}{PieceType.ToString()}{Rank.ToString()}".GetHashCode();

        public override string ToString() =>
            string.Join(" ",
                ChessFormatter.PieceToString(PieceType, pForPawn: true),
                (File.HasValue && Rank.HasValue) ? $"{ChessFormatter.FileToString(File.Value)}{ChessFormatter.RankToString(Rank.Value)}" : null);
    }
}

using System;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    public class BoardPosition : IEquatable<BoardPosition>
    {
        public int File { get; set; }

        public int Rank { get; set; }

        public BoardPosition(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public override string ToString() =>
            $"{ChessFormatter.FileToString(File)}{ChessFormatter.RankToString(Rank)}";

        public bool Equals(BoardPosition? other) => File == other?.File && Rank == other?.Rank;

        public override bool Equals(object obj) => (obj is BoardPosition pos) ? Equals(pos) : false;

        public override int GetHashCode() => (File * 100) + Rank;

        public static bool operator ==(BoardPosition? lhs, BoardPosition? rhs)
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

        public static bool operator !=(BoardPosition? lhs, BoardPosition? rhs)
        {
            return !(lhs == rhs);
        }
    }
}

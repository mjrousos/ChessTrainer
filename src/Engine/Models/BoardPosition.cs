using System;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public int File { get; }

        public int Rank { get; }

        public BoardPosition(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public BoardPosition(string rankAndFile)
        {
            if (string.IsNullOrWhiteSpace(rankAndFile))
            {
                throw new ArgumentException("Rank and file cannot be null or empty", nameof(rankAndFile));
            }

            if (rankAndFile.Length != 2)
            {
                throw new ArgumentException("Rank and file strings must contain exactly two characters", nameof(rankAndFile));
            }

            File = ChessFormatter.FileFromChar(rankAndFile[0]);
            Rank = ChessFormatter.RankFromChar(rankAndFile[1]);
        }

        public override string ToString() =>
            $"{ChessFormatter.FileToString(File)}{ChessFormatter.RankToString(Rank)}";

        // Even though value types will do equality correctly automatically, custom implementations are
        // faster since the default uses reflection.
        public bool Equals(BoardPosition other) => File == other.File && Rank == other.Rank;

        public override bool Equals(object obj) => (obj is BoardPosition pos) ? Equals(pos) : false;

        public override int GetHashCode() => (File * 100) + Rank;

        public static bool operator ==(BoardPosition lhs, BoardPosition rhs) => lhs.Equals(rhs);

        public static bool operator !=(BoardPosition lhs, BoardPosition rhs) => !(lhs == rhs);
    }
}

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

        public bool Equals(BoardPosition other) => File == other.File && Rank == other.Rank;

        public override bool Equals(object obj) => (obj is BoardPosition pos) ? Equals(pos) : false;

        public override int GetHashCode() => (File * 100) + Rank;
    }
}

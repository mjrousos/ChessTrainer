using System;
using System.Collections.Generic;
using MjrChess.Engine.Models;

namespace MjrChess.Trainer.Models
{
    public class TacticsPuzzle : EntityBase
    {
        public TacticsPuzzle(string position)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                throw new ArgumentException("message", nameof(position));
            }

            Position = position;
        }

        public string Position { get; set; }

        public ChessPieces PieceMoved { get; set; }

        public string MovedFrom { get; set; } = default!; // https://github.com/dotnet/csharplang/issues/2869

        public string MovedTo { get; set; } = default!; // https://github.com/dotnet/csharplang/issues/2869

        public Move Solution
        {
            get => new Move(PieceMoved, new BoardPosition(MovedFrom), new BoardPosition(MovedTo));
            set
            {
                PieceMoved = value.PieceMoved;
                MovedFrom = value.OriginalPosition.ToString();
                MovedTo = value.FinalPosition.ToString();
            }
        }

        public string? WhitePlayer { get; set; }

        public string? BlackPlayer { get; set; }

        public DateTimeOffset? GameDate { get; set; }

        public ICollection<PuzzleHistory> History { get; set; } = new HashSet<PuzzleHistory>();
    }
}

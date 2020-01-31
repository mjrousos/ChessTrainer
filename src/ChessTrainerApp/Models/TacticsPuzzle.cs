using System;
using System.Collections.Generic;
using MjrChess.Engine.Models;

namespace MjrChess.Trainer.Models
{
    public class TacticsPuzzle : IEntity
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

        public ChessPieces? IncorrectPieceMoved { get; set; }

        public string? IncorrectMovedFrom { get; set; }

        public string? IncorrectMovedTo { get; set; }

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

        public Move? IncorrectMove
        {
            get => (IncorrectPieceMoved != null && IncorrectMovedFrom != null && IncorrectMovedTo != null) ?
                new Move(IncorrectPieceMoved.Value, new BoardPosition(IncorrectMovedFrom), new BoardPosition(IncorrectMovedTo)) :
                null;
            set
            {
                IncorrectPieceMoved = value?.PieceMoved;
                IncorrectMovedFrom = value?.OriginalPosition.ToString();
                IncorrectMovedTo = value?.FinalPosition.ToString();
            }
        }

        public Player? WhitePlayer { get; set; }

        public Player? BlackPlayer { get; set; }

        public DateTimeOffset? GameDate { get; set; }

        public string? Site { get; set; }

        public ICollection<PuzzleHistory> History { get; set; } = new HashSet<PuzzleHistory>();
    }
}

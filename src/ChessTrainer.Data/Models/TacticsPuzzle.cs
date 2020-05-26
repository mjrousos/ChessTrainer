using System;
using System.Collections.Generic;
using MjrChess.Engine.Models;

namespace MjrChess.Trainer.Data.Models
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

        public string SetupMovedFrom { get; set; } = default!;

        public string SetupMovedTo { get; set; } = default!;

        public ChessPieces? SetupPiecePromotedTo { get; set; }

        public string MovedFrom { get; set; } = default!; // https://github.com/dotnet/csharplang/issues/2869

        public string MovedTo { get; set; } = default!; // https://github.com/dotnet/csharplang/issues/2869

        public ChessPieces? PiecePromotedTo { get; set; }

        public string? IncorrectMovedFrom { get; set; }

        public string? IncorrectMovedTo { get; set; }

        public ChessPieces? IncorrectPiecePromotedTo { get; set; }

        public Player? WhitePlayer { get; set; }

        public Player? BlackPlayer { get; set; }

        public DateTimeOffset? GameDate { get; set; }

        public string? Site { get; set; }

        public string? GameUrl { get; set; }

        public ICollection<PuzzleHistory> History { get; set; } = new HashSet<PuzzleHistory>();
    }
}

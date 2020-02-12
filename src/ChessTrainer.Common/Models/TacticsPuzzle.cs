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

        public ChessPieces SetupPieceMoved { get; set; } = default!;

        public string SetupMovedFrom { get; set; } = default!;

        public string SetupMovedTo { get; set; } = default!;

        public ChessPieces? SetupPiecePromotedTo { get; set; }

        public ChessPieces PieceMoved { get; set; }

        public string MovedFrom { get; set; } = default!; // https://github.com/dotnet/csharplang/issues/2869

        public string MovedTo { get; set; } = default!; // https://github.com/dotnet/csharplang/issues/2869

        public ChessPieces? PiecePromotedTo { get; set; }

        public ChessPieces? IncorrectPieceMoved { get; set; }

        public string? IncorrectMovedFrom { get; set; }

        public string? IncorrectMovedTo { get; set; }

        public ChessPieces? IncorrectPiecePromotedTo { get; set; }

        public Move Solution
        {
            get => new Move(PieceMoved, new BoardPosition(MovedFrom), new BoardPosition(MovedTo), PiecePromotedTo);
            set
            {
                PieceMoved = value.PieceMoved;
                MovedFrom = value.OriginalPosition.ToString();
                MovedTo = value.FinalPosition.ToString();
                PiecePromotedTo = value.PiecePromotedTo;
            }
        }

        public Move? IncorrectMove
        {
            get => (IncorrectPieceMoved != null && IncorrectMovedFrom != null && IncorrectMovedTo != null) ?
                new Move(IncorrectPieceMoved.Value, new BoardPosition(IncorrectMovedFrom), new BoardPosition(IncorrectMovedTo), IncorrectPiecePromotedTo) :
                null;
            set
            {
                IncorrectPieceMoved = value?.PieceMoved;
                IncorrectMovedFrom = value?.OriginalPosition.ToString();
                IncorrectMovedTo = value?.FinalPosition.ToString();
                IncorrectPiecePromotedTo = value?.PiecePromotedTo;
            }
        }

        public Move SetupMove
        {
            get => new Move(SetupPieceMoved, new BoardPosition(SetupMovedFrom), new BoardPosition(SetupMovedTo), SetupPiecePromotedTo);
            set
            {
                SetupPieceMoved = value.PieceMoved;
                SetupMovedFrom = value.OriginalPosition.ToString();
                SetupMovedTo = value.FinalPosition.ToString();
                SetupPiecePromotedTo = value.PiecePromotedTo;
            }
        }

        public Player? WhitePlayer { get; set; }

        public Player? BlackPlayer { get; set; }

        public DateTimeOffset? GameDate { get; set; }

        public string? Site { get; set; }

        public string? GameUrl { get; set; }

        public ICollection<PuzzleHistory> History { get; set; } = new HashSet<PuzzleHistory>();
    }
}

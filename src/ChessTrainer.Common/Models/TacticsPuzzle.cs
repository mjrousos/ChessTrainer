using System;
using System.Collections.Generic;
using MjrChess.Engine;
using MjrChess.Engine.Models;
using MjrChess.Engine.Utilities;

namespace MjrChess.Trainer.Models
{
    public class TacticsPuzzle : IEntity
    {
        public TacticsPuzzle(
            string position,
            string setupMovedFrom,
            string setupMovedTo,
            ChessPieces? setupPiecePromotedTo,
            string movedFrom,
            string movedTo,
            ChessPieces? piecePromotedTo,
            string? incorrectMovedFrom,
            string? incorrectMovedTo,
            ChessPieces? incorrectPiecePromotedTo)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                throw new ArgumentException("message", nameof(position));
            }

            Position = position;
            SetupMovedFrom = setupMovedFrom;
            SetupMovedTo = setupMovedTo;
            SetupPiecePromotedTo = setupPiecePromotedTo;
            MovedFrom = movedFrom;
            MovedTo = movedTo;
            PiecePromotedTo = piecePromotedTo;
            IncorrectMovedFrom = incorrectMovedFrom;
            IncorrectMovedTo = incorrectMovedTo;
            IncorrectPiecePromotedTo = incorrectPiecePromotedTo;

            var engine = new ChessEngine();
            engine.LoadFEN(Position);
            SetupMove = engine.MoveFromAlgebraicNotation($"{SetupMovedFrom}{SetupMovedTo}{(SetupPiecePromotedTo.HasValue ? $"={ChessFormatter.PieceToString(SetupPiecePromotedTo.Value, false)}" : string.Empty)}");
            engine.Game.Move(SetupMove);
            Solution = engine.MoveFromAlgebraicNotation($"{MovedFrom}{MovedTo}{(PiecePromotedTo.HasValue ? $"={ChessFormatter.PieceToString(PiecePromotedTo.Value, false)}" : string.Empty)}");
            if (IncorrectMovedFrom != null && IncorrectMovedTo != null)
            {
                IncorrectMove = engine.MoveFromAlgebraicNotation($"{IncorrectMovedFrom}{IncorrectMovedTo}{(IncorrectPiecePromotedTo.HasValue ? $"={ChessFormatter.PieceToString(IncorrectPiecePromotedTo.Value, false)}" : string.Empty)}");
            }
        }

        public string Position { get; }

        public string SetupMovedFrom { get; }

        public string SetupMovedTo { get; }

        public ChessPieces? SetupPiecePromotedTo { get; }

        public string MovedFrom { get; }

        public string MovedTo { get; }

        public ChessPieces? PiecePromotedTo { get; }

        public string? IncorrectMovedFrom { get; }

        public string? IncorrectMovedTo { get; }

        public ChessPieces? IncorrectPiecePromotedTo { get; }

        // Return the opposite of what the position FEN indicates, since that's the state
        // before the setup move is made
        public bool WhiteToMove => !Position.Split()[1].Equals("w", StringComparison.OrdinalIgnoreCase);

        public Move Solution { get; }

        public Move? IncorrectMove { get; }

        public Move SetupMove { get; }

        public string? WhitePlayerName { get; set; }

        public string? BlackPlayerName { get; set; }

        public int AssociatedPlayerId { get; set; }

        public DateTimeOffset? GameDate { get; set; }

        public string? Site { get; set; }

        public string? GameUrl { get; set; }

        public ICollection<PuzzleHistory> History { get; set; } = new HashSet<PuzzleHistory>();
    }
}

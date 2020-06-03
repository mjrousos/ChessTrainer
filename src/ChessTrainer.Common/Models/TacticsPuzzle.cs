using System;
using System.Collections.Generic;
using MjrChess.Engine.Models;

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

            var game = new ChessGame();
            game.LoadFEN(Position);
            SetupPieceMoved = game.GetPiece(new BoardPosition(SetupMovedFrom))?.PieceType ?? throw new InvalidOperationException($"Invalid puzzle; no piece at position {SetupMovedFrom} ({game.GetFEN()})");
            game.Move(SetupMove);

            PieceMoved = game.GetPiece(new BoardPosition(MovedFrom))?.PieceType ?? throw new InvalidOperationException($"Invalid puzzle; no piece at position {MovedFrom} ({game.GetFEN()})");
            IncorrectPieceMoved = IncorrectMovedFrom == null ? (ChessPieces?)null :
                game.GetPiece(new BoardPosition(IncorrectMovedFrom))?.PieceType ?? throw new InvalidOperationException($"Invalid puzzle; no piece at position {IncorrectMovedFrom} ({game.GetFEN()})");
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

        private ChessPieces SetupPieceMoved { get; }

        private ChessPieces? IncorrectPieceMoved { get; }

        private ChessPieces PieceMoved { get; }

        public Move Solution
        {
            get => new Move(PieceMoved, new BoardPosition(MovedFrom), new BoardPosition(MovedTo), PiecePromotedTo);
        }

        public Move? IncorrectMove
        {
            get => (IncorrectPieceMoved != null && IncorrectMovedFrom != null && IncorrectMovedTo != null) ?
                new Move(IncorrectPieceMoved.Value, new BoardPosition(IncorrectMovedFrom), new BoardPosition(IncorrectMovedTo), IncorrectPiecePromotedTo) :
                null;
        }

        public Move SetupMove
        {
            get => new Move(SetupPieceMoved, new BoardPosition(SetupMovedFrom), new BoardPosition(SetupMovedTo), SetupPiecePromotedTo);
        }

        public string? WhitePlayerName { get; set; }

        public string? BlackPlayerName { get; set; }

        public int AssociatedPlayerId { get; set; }

        public DateTimeOffset? GameDate { get; set; }

        public string? Site { get; set; }

        public string? GameUrl { get; set; }

        public ICollection<PuzzleHistory> History { get; set; } = new HashSet<PuzzleHistory>();
    }
}

using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    /// <summary>
    /// A single move in a chess game.
    /// </summary>
    public class Move
    {
        // https://github.com/dotnet/csharplang/issues/2328
        public Move(ChessPieces pieceMoved, BoardPosition originalPosition, BoardPosition finalPosition)
        {
            PieceMoved = pieceMoved;
            OriginalPosition = originalPosition;
            FinalPosition = finalPosition;
        }

        public BoardPosition OriginalPosition { get; set; }

        public BoardPosition FinalPosition { get; set; }

        public bool AmbiguousOriginalRank { get; set; }

        public bool AmbiguousOriginalFile { get; set; }

        public bool Capture { get; set; }

        public bool Checks { get; set; }

        public bool Checkmates { get; set; }

        public bool Stalemates { get; set; }

        public bool ShortCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalPosition.File == 4 &&
            FinalPosition.File == 6;

        public bool LongCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalPosition.File == 4 &&
            FinalPosition.File == 2;

        public ChessPieces PieceMoved { get; set; }

        public ChessPieces? PiecePromotedTo { get; set; }

        public override string ToString() => ChessFormatter.MoveToString(this);
    }
}

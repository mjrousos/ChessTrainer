using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    /// <summary>
    /// A single move in a chess game.
    /// </summary>
    public class Move
    {
        public int OriginalRank { get; set; }

        public int OriginalFile { get; set; }

        public int FinalRank { get; set; }

        public int FinalFile { get; set; }

        public bool AmbiguousOriginalRank { get; set; }

        public bool AmbiguousOriginalFile { get; set; }

        public bool Capture { get; set; }

        public bool Checks { get; set; }

        public bool Checkmates { get; set; }

        public bool ShortCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalFile == 4 &&
            FinalFile == 6;

        public bool LongCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalFile == 4 &&
            FinalFile == 2;

        public ChessPieces PieceMoved { get; set; }

        public ChessPieces? PiecePromotedTo { get; set; }

        public override string ToString() => ChessFormatter.MoveToString(this);
    }
}

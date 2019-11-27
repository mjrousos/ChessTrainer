namespace ChessTrainerApp
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
            OriginalRank == 3 &&
            FinalRank == 1;

        public bool LongCastle =>
            (PieceMoved == ChessPieces.WhiteKing || PieceMoved == ChessPieces.BlackKing) &&
            OriginalRank == 3 &&
            FinalRank == 5;

        public ChessPieces PieceMoved { get; set; }

        public ChessPieces? PiecePromotedTo { get; set; }

        public override string ToString() => ChessFormatter.MoveToString(this);
    }
}

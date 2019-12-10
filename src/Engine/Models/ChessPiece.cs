using System;
using MjrChess.Engine.Utilities;

namespace MjrChess.Engine.Models
{
    public class ChessPiece
    {
        public ChessPiece(ChessPieces pieceType, int file, int rank)
        {
            PieceType = pieceType;
            File = file;
            Rank = rank;
        }

        public ChessPieces PieceType { get; set; }

        public int? File { get; set; }

        public int? Rank { get; set; }

        public override string ToString() => 
            String.Join(" ", 
                ChessFormatter.PieceToString(PieceType, pForPawn: true), 
                (File.HasValue && Rank.HasValue)? $"{ChessFormatter.FileToString(File.Value)}{ChessFormatter.RankToString(Rank.Value)}" : null);
    }
}

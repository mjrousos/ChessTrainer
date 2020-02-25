using System;
using System.Linq;
using MjrChess.Engine.Models;
using MjrChess.Engine.Utilities;
using MjrChess.Trainer.Models;

namespace IngestionFunctions.Models
{
    /// <summary>
    /// A request to process a game for potential tactical puzzles.
    /// </summary>
    public class IngestionRequest
    {
        public ChessSites Site { get; set; }

        public DateTimeOffset GameDate { get; set; }

        public string? GameUrl { get; set; }

        public string WhitePlayer { get; set; }

        public string BlackPlayer { get; set; }

        public string[] UCIMoves { get; set; }

        public IngestionRequest(ChessGame game, ChessSites site, string? gamePath)
        {
            Site = site;
            GameDate = game.StartDate;
            GameUrl = gamePath;
            WhitePlayer = game.WhitePlayer;
            BlackPlayer = game.BlackPlayer;
            UCIMoves = game.Moves.Select(m => ChessFormatter.MoveToUCINotation(m)).ToArray();
        }
    }
}

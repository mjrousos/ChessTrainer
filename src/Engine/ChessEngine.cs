using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MjrChess.Engine.Models;

namespace MjrChess.Engine
{
    /// <summary>
    /// Analysis engine for determining legal moves, best moves, etc. from a chess position.
    /// </summary>
    public class ChessEngine
    {
        public ChessGame Game { get; internal set; }

        // This is created as an instance class (even though identifying legal moves
        // could easily be accomplished by a static class) to facilitate more easily expanding
        // this class to a full UCI-compatible analysis engine in the future (which makes sense
        // to have an instance class for, for pondering, etc.).
        // UCI Spec: http://download.shredderchess.com/div/uci.zip
        public ChessEngine()
        {
            Game = new ChessGame();
        }

        /// <summary>
        /// Loads chess game into the analysis engine.
        /// </summary>
        /// <param name="fen">A FEN description of the game to load.</param>
        public void LoadPosition(string fen)
        {
            Game.LoadFEN(fen);
        }

        /// <summary>
        /// Loads chess game into the analysis engine.
        /// </summary>
        /// <param name="game">The game to load.</param>
        public void LoadPosition(ChessGame game)
        {
            Game = game;
        }

        /// <summary>
        /// Retrieves potential legal moves for a given piece.
        /// </summary>
        /// <param name="pieceToMove">The piece to move.</param>
        /// <returns>All possible legal moves for the piece.</returns>
        public IEnumerable<Move> GetLegalMoves(ChessPiece pieceToMove)
        {
            // If no piece is passed in, return no moves
            if (pieceToMove == null)
            {
                yield break;
            }

            // If a piece not present in the game state is passed in, throw
            if (!(Game?.Pieces.Contains(pieceToMove) ?? false))
            {
                throw new ArgumentException("Piece to find legal moves for must be present in the game", nameof(pieceToMove));
            }

            // TODO - TEMPORARILY RETURN RANDOM SQUARES AS LEGAL. THIS MUST BE FIXED.
            var hasher = MD5.Create();
            var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(pieceToMove.ToString()));
            var numGen = new Random(BitConverter.ToInt32(hash, 0));
            for (var i = 0; i < Game.BoardSize; i++)
            {
                for (var j = 0; j < Game.BoardSize; j++)
                {
                    if (numGen.Next(3) == 0)
                    {
                        yield return new Move
                        {
                            PieceMoved = pieceToMove.PieceType,
                            OriginalFile = pieceToMove.File.Value,
                            OriginalRank = pieceToMove.Rank.Value,
                            FinalFile = i,
                            FinalRank = j
                        };
                    }
                }
            }
        }
    }
}

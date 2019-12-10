using System;
using System.Collections.Generic;
using System.Linq;
using MjrChess.Engine.Models;
using MjrChess.Engine.Utilities;

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
            if (pieceToMove != null)
            {
                foreach (var possibleMove in GetMoveOptions(pieceToMove))
                {
                    var move = ValidateMove(possibleMove);
                    if (move != null)
                    {
                        yield return move;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all possible squares a piece can move to, not considering
        /// whether the move is legal (due to check) or whether the move results
        /// in check, checkmate, or an ambiguous move.
        /// </summary>
        /// <param name="piece">The piece to move.</param>
        /// <returns>A complete list of possible ending squares.</returns>
        public IEnumerable<Move> GetMoveOptions(ChessPiece piece) =>
            piece.PieceType switch
            {
                ChessPieces.WhitePawn => GetPawnMoves(piece),
                ChessPieces.BlackPawn => GetPawnMoves(piece),
                _ => Enumerable.Empty<Move>()
            };

        /// <summary>
        /// Checks a possible move for check, checkmate, and ambiguous moves.
        /// </summary>
        /// <param name="move">The move to validate.</param>
        /// <returns>Null if the move is illegal or, if legal, an updated move with check, checkmate, and ambiguous move information.</returns>
        private Move ValidateMove(Move move)
        {
            // TODO
            return move;
        }

        /// <summary>
        /// Gets possible (unvalidated) moves for a pawn based on board position.
        /// </summary>
        /// <remarks>If the pawn would be promoted, indicate queen promotion with the intention that the caller will see this and adjust, if necessary.</remarks>
        /// <param name="pawn">The pawn to move.</param>
        /// <returns>Possible squares to move to (considering the position of the pawn and other pieces, but not considering check).</returns>
        private IEnumerable<Move> GetPawnMoves(ChessPiece pawn)
        {
            var whitePawn = ChessFormatter.IsPieceWhite(pawn.PieceType);
            var rankProgresion = whitePawn ? 1 : -1;

            // Check one-space advance
            var finalFile = pawn.File.Value;
            var finalRank = pawn.Rank.Value + rankProgresion;
            if (Game.GetPiece(finalFile, finalRank) == null)
            {
                yield return GetPawnMove(pawn, finalFile, finalRank, false);

                // Check two-space advance
                finalFile = pawn.File.Value;
                finalRank = pawn.Rank.Value + (2 * rankProgresion);
                if (pawn.Rank.Value == (whitePawn ? 1 : 6) && Game.GetPiece(finalFile, finalRank) == null)
                {
                    yield return GetPawnMove(pawn, finalFile, finalRank, false);
                }
            }

            // Check captures
            var captureFiles = new[] { pawn.File.Value - 1, pawn.File.Value + 1 };
            foreach (var captureFile in captureFiles)
            {
                finalFile = captureFile;
                finalRank = pawn.Rank.Value + rankProgresion;
                var pieceAtDestination = Game.GetPiece(finalFile, finalRank);
                if (Game.EnPassantTarget == (finalFile, finalRank) || (pieceAtDestination != null && ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType) != whitePawn))
                {
                    yield return GetPawnMove(pawn, finalFile, finalRank, true);
                }
            }

            Move GetPawnMove(ChessPiece pawn, int finalFile, int finalRank, bool capture)
            {
                var promoted = finalRank % (Game.BoardSize - 1) == 0;
                return new Move
                {
                    PieceMoved = pawn.PieceType,
                    OriginalFile = pawn.File.Value,
                    OriginalRank = pawn.Rank.Value,
                    FinalFile = finalFile,
                    FinalRank = finalRank,
                    Capture = capture,
                    PiecePromotedTo = promoted ? (whitePawn ? ChessPieces.WhiteQueen : ChessPieces.BlackQueen) : (ChessPieces?)null
                };
            }
        }
    }
}

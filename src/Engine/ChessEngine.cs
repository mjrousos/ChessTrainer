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
        private static IEnumerable<Func<int, int, (int, int)>> RookMovements => new Func<int, int, (int, int)>[]
        {
            (file, rank) => (file + 1, rank),
            (file, rank) => (file - 1, rank),
            (file, rank) => (file, rank + 1),
            (file, rank) => (file, rank - 1)
        };

        private static IEnumerable<Func<int, int, (int, int)>> BishopMovements => new Func<int, int, (int, int)>[]
        {
            (file, rank) => (file + 1, rank + 1),
            (file, rank) => (file - 1, rank + 1),
            (file, rank) => (file + 1, rank - 1),
            (file, rank) => (file - 1, rank - 1)
        };

        private static IEnumerable<Func<int, int, (int, int)>> KnightMovements => new Func<int, int, (int, int)>[]
        {
            (file, rank) => (file + 2, rank + 1),
            (file, rank) => (file + 2, rank - 1),
            (file, rank) => (file - 2, rank + 1),
            (file, rank) => (file - 2, rank - 1),
            (file, rank) => (file + 1, rank + 2),
            (file, rank) => (file - 1, rank + 2),
            (file, rank) => (file + 1, rank - 2),
            (file, rank) => (file - 1, rank - 2)
        };

        private static IEnumerable<Func<int, int, (int, int)>> RoyalMovements => RookMovements.Union(BishopMovements);

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
                ChessPieces.WhiteRook => GetRookMoves(piece),
                ChessPieces.BlackRook => GetRookMoves(piece),
                ChessPieces.WhiteBishop => GetBishopMoves(piece),
                ChessPieces.BlackBishop => GetBishopMoves(piece),
                ChessPieces.WhiteQueen => GetQueenMoves(piece),
                ChessPieces.BlackQueen => GetQueenMoves(piece),
                ChessPieces.WhiteKnight => GetKnightMoves(piece),
                ChessPieces.BlackKnight => GetKnightMoves(piece),
                ChessPieces.WhiteKing => GetKingMoves(piece),
                ChessPieces.BlackKing => GetKingMoves(piece),
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
                yield return CreateMoveFromPiece(pawn, finalFile, finalRank, false);

                // Check two-space advance
                finalFile = pawn.File.Value;
                finalRank = pawn.Rank.Value + (2 * rankProgresion);
                if (pawn.Rank.Value == (whitePawn ? 1 : 6) && Game.GetPiece(finalFile, finalRank) == null)
                {
                    yield return CreateMoveFromPiece(pawn, finalFile, finalRank, false);
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
                    yield return CreateMoveFromPiece(pawn, finalFile, finalRank, true);
                }
            }
        }

        private IEnumerable<Move> GetKingMoves(ChessPiece king)
        {
            var whiteKing = ChessFormatter.IsPieceWhite(king.PieceType);

            // Check for castling
            if (whiteKing)
            {
                if ((Game.WhiteCastlingOptions & CastlingOptions.KingSide) == CastlingOptions.KingSide &&
                    Game.GetPiece(5, 0) == null &&
                    Game.GetPiece(6, 0) == null)
                {
                    yield return CreateMoveFromPiece(king, 6, 0, false);
                }
                else if ((Game.WhiteCastlingOptions & CastlingOptions.QueenSide) == CastlingOptions.QueenSide &&
                    Game.GetPiece(3, 0) == null &&
                    Game.GetPiece(2, 0) == null &&
                    Game.GetPiece(1, 0) == null)
                {
                    yield return CreateMoveFromPiece(king, 2, 0, false);
                }
            }
            else
            {
                if ((Game.BlackCastlingOptions & CastlingOptions.KingSide) == CastlingOptions.KingSide &&
                    Game.GetPiece(5, 7) == null &&
                    Game.GetPiece(6, 7) == null)
                {
                    yield return CreateMoveFromPiece(king, 6, 7, false);
                }
                else if ((Game.BlackCastlingOptions & CastlingOptions.QueenSide) == CastlingOptions.QueenSide &&
                    Game.GetPiece(3, 7) == null &&
                    Game.GetPiece(2, 7) == null &&
                    Game.GetPiece(1, 7) == null)
                {
                    yield return CreateMoveFromPiece(king, 2, 7, false);
                }
            }

            // Check for normal moves
            foreach (var applyMovement in RoyalMovements)
            {
                var (finalFile, finalRank) = applyMovement(king.File.Value, king.Rank.Value);
                if (finalFile >= 0 && finalFile < Game.BoardSize && finalRank >= 0 && finalRank < Game.BoardSize)
                {
                    var pieceAtDestination = Game.GetPiece(finalFile, finalRank);
                    if (pieceAtDestination == null)
                    {
                        yield return CreateMoveFromPiece(king, finalFile, finalRank, false);
                    }
                    else if (whiteKing != ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType))
                    {
                        yield return CreateMoveFromPiece(king, finalFile, finalRank, true);
                    }
                }
            }
        }

        private IEnumerable<Move> GetKnightMoves(ChessPiece knight)
        {
            foreach (var applyMovement in KnightMovements)
            {
                var (finalFile, finalRank) = applyMovement(knight.File.Value, knight.Rank.Value);
                if (finalFile >= 0 && finalFile < Game.BoardSize && finalRank >= 0 && finalRank < Game.BoardSize)
                {
                    var pieceAtDestination = Game.GetPiece(finalFile, finalRank);
                    if (pieceAtDestination == null)
                    {
                        yield return CreateMoveFromPiece(knight, finalFile, finalRank, false);
                    }
                    else if (ChessFormatter.IsPieceWhite(knight.PieceType) != ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType))
                    {
                        yield return CreateMoveFromPiece(knight, finalFile, finalRank, true);
                    }
                }
            }
        }

        private IEnumerable<Move> GetRookMoves(ChessPiece rook) => GetStraightPieceMoves(rook, RookMovements);

        private IEnumerable<Move> GetBishopMoves(ChessPiece rook) => GetStraightPieceMoves(rook, BishopMovements);

        private IEnumerable<Move> GetQueenMoves(ChessPiece rook) => GetStraightPieceMoves(rook, RoyalMovements);

        private IEnumerable<Move> GetStraightPieceMoves(ChessPiece piece, IEnumerable<Func<int, int, (int, int)>> movements)
        {
            foreach (var applyMovement in movements)
            {
                var (finalFile, finalRank) = applyMovement(piece.File.Value, piece.Rank.Value);

                while (finalFile >= 0 && finalFile < Game.BoardSize && finalRank >= 0 && finalRank < Game.BoardSize)
                {
                    var pieceAtDestination = Game.GetPiece(finalFile, finalRank);
                    if (pieceAtDestination == null)
                    {
                        yield return CreateMoveFromPiece(piece, finalFile, finalRank, false);
                    }
                    else if (ChessFormatter.IsPieceWhite(piece.PieceType) != ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType))
                    {
                        yield return CreateMoveFromPiece(piece, finalFile, finalRank, true);
                        break;
                    }
                    else
                    {
                        break;
                    }

                    (finalFile, finalRank) = applyMovement(finalFile, finalRank);
                }
            }
        }

        private Move CreateMoveFromPiece(ChessPiece piece, int finalFile, int finalRank, bool capture)
        {
            var promoted = (piece.PieceType == ChessPieces.WhitePawn || piece.PieceType == ChessPieces.BlackPawn) && finalRank % (Game.BoardSize - 1) == 0;
            return new Move
            {
                PieceMoved = piece.PieceType,
                OriginalFile = piece.File.Value,
                OriginalRank = piece.Rank.Value,
                FinalFile = finalFile,
                FinalRank = finalRank,
                Capture = capture,
                PiecePromotedTo = promoted ? (ChessFormatter.IsPieceWhite(piece.PieceType) ? ChessPieces.WhiteQueen : ChessPieces.BlackQueen) : (ChessPieces?)null
            };
        }
    }
}

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
        private static IEnumerable<Func<BoardPosition, BoardPosition>> RookMovements => new Func<BoardPosition, BoardPosition>[]
        {
            position => new BoardPosition(position.File + 1, position.Rank),
            position => new BoardPosition(position.File - 1, position.Rank),
            position => new BoardPosition(position.File, position.Rank + 1),
            position => new BoardPosition(position.File, position.Rank - 1)
        };

        private static IEnumerable<Func<BoardPosition, BoardPosition>> BishopMovements => new Func<BoardPosition, BoardPosition>[]
        {
            position => new BoardPosition(position.File + 1, position.Rank + 1),
            position => new BoardPosition(position.File - 1, position.Rank + 1),
            position => new BoardPosition(position.File + 1, position.Rank - 1),
            position => new BoardPosition(position.File - 1, position.Rank - 1)
        };

        private static IEnumerable<Func<BoardPosition, BoardPosition>> KnightMovements => new Func<BoardPosition, BoardPosition>[]
        {
            position => new BoardPosition(position.File + 2, position.Rank + 1),
            position => new BoardPosition(position.File + 2, position.Rank - 1),
            position => new BoardPosition(position.File - 2, position.Rank + 1),
            position => new BoardPosition(position.File - 2, position.Rank - 1),
            position => new BoardPosition(position.File + 1, position.Rank + 2),
            position => new BoardPosition(position.File - 1, position.Rank + 2),
            position => new BoardPosition(position.File + 1, position.Rank - 2),
            position => new BoardPosition(position.File - 1, position.Rank - 2)
        };

        private static IEnumerable<Func<BoardPosition, BoardPosition>> RoyalMovements => RookMovements.Union(BishopMovements);

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
                var possibleMoves = GetMoveOptions(pieceToMove).Where(IsLegal);
                foreach (var move in possibleMoves)
                {
                    CheckForAmbiguousMove(move);
                    CheckForCheck(move);
                    CheckForMate(move);
                    yield return move;
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
            piece?.PieceType switch
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

        private void CheckForAmbiguousMove(Move move)
        {
            var similarPieces = Game.Pieces.Where(p => p.PieceType == move.PieceMoved);

            foreach (var piece in similarPieces)
            {
                if (piece.Position == move.OriginalPosition)
                {
                    continue;
                }

                if (GetMoveOptions(piece).Where(IsLegal).Select(m => m.FinalPosition).Contains(move.FinalPosition))
                {
                    if (piece.Position.File != move.OriginalPosition.File)
                    {
                        move.AmbiguousOriginalFile = true;
                    }
                    else
                    {
                        move.AmbiguousOriginalRank = true;
                    }
                }
            }
        }

        private void CheckForCheck(Move move)
        {
            var whiteMoving = ChessFormatter.IsPieceWhite(move.PieceMoved);

            // Create a hypothetical board state if the propose move was made
            var prospectiveGame = new ChessGame();
            prospectiveGame.LoadFEN(Game.GetFEN());
            prospectiveGame.Move(move);
            var prospectiveEngine = new ChessEngine { Game = prospectiveGame };

            var friendlyPieces = prospectiveGame.Pieces.Where(p => ChessFormatter.IsPieceWhite(p.PieceType) == whiteMoving);
            var opposingKing = prospectiveGame.Pieces.Single(p => p.PieceType == (whiteMoving ? ChessPieces.BlackKing : ChessPieces.WhiteKing));

            foreach (var piece in friendlyPieces)
            {
                // If any friendly piece could move to the opposing king's square, then the move gives check
                // We don't need to check for legal moves because even pinned pieces or others that can't move because
                // of a risk of check from the opponent are still able to give check.
                if (prospectiveEngine.GetMoveOptions(piece).Select(m => m.FinalPosition).Contains(opposingKing.Position))
                {
                    move.Checks = true;
                    if (move.Stalemates)
                    {
                        move.Stalemates = false;
                        move.Checkmates = true;
                    }

                    break;
                }
            }
        }

        private void CheckForMate(Move move)
        {
            // Create a hypothetical board state if the propose move was made
            var prospectiveGame = new ChessGame();
            prospectiveGame.LoadFEN(Game.GetFEN());
            prospectiveGame.Move(move);
            var prospectiveEngine = new ChessEngine { Game = prospectiveGame };
            var opposingPieces = prospectiveGame.Pieces.Where(p => ChessFormatter.IsPieceWhite(p.PieceType) != ChessFormatter.IsPieceWhite(move.PieceMoved));

            // Calculate all possible legal moves for the opponent after the proposed move. If there are no moves available, then it is mate.
            // If I develop this into a fuller engine in the future, this method may be absorbed into more general calculating functionality.
            var possibleMoves = opposingPieces.SelectMany(p => prospectiveEngine.GetMoveOptions(p).Where(prospectiveEngine.IsLegal));

            if (!possibleMoves.Any())
            {
                if (move.Checks)
                {
                    move.Checkmates = true;
                }
                else
                {
                    move.Stalemates = true;
                }
            }
        }

        private bool IsLegal(Move move)
        {
            var whiteMoving = ChessFormatter.IsPieceWhite(move.PieceMoved);

            // Create a hypothetical board state if the propose move was made
            var prospectiveGame = new ChessGame();
            prospectiveGame.LoadFEN(Game.GetFEN());
            prospectiveGame.Move(move);
            var prospectiveEngine = new ChessEngine { Game = prospectiveGame };

            var opposingPieces = prospectiveGame.Pieces.Where(p => ChessFormatter.IsPieceWhite(p.PieceType) != whiteMoving);
            var friendlyKing = prospectiveGame.Pieces.Single(p => p.PieceType == (whiteMoving ? ChessPieces.WhiteKing : ChessPieces.BlackKing));

            // If any of the opponent's pieces could capture the king after this move, then it is illegal
            var kingVulnerable = opposingPieces.Any(piece => prospectiveEngine.GetMoveOptions(piece).Select(m => m.FinalPosition).Contains(friendlyKing.Position));

            return !kingVulnerable;
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
            var finalPosition = new BoardPosition(pawn.Position.File, pawn.Position.Rank + rankProgresion);
            if (Game.GetPiece(finalPosition) == null)
            {
                yield return CreateMoveFromPiece(pawn, finalPosition, false);

                // Check two-space advance
                finalPosition = new BoardPosition(pawn.Position.File, pawn.Position.Rank + (2 * rankProgresion));
                if (pawn.Position.Rank == (whitePawn ? 1 : 6) && Game.GetPiece(finalPosition) == null)
                {
                    yield return CreateMoveFromPiece(pawn, finalPosition, false);
                }
            }

            // Check captures
            var captureFiles = new[] { pawn.Position.File - 1, pawn.Position.File + 1 };
            foreach (var captureFile in captureFiles)
            {
                finalPosition = new BoardPosition(captureFile, pawn.Position.Rank + rankProgresion);
                var pieceAtDestination = Game.GetPiece(finalPosition);
                if (Game.EnPassantTarget == finalPosition || (pieceAtDestination != null && ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType) != whitePawn))
                {
                    yield return CreateMoveFromPiece(pawn, finalPosition, true);
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
                    yield return CreateMoveFromPiece(king, new BoardPosition(6, 0), false);
                }

                if ((Game.WhiteCastlingOptions & CastlingOptions.QueenSide) == CastlingOptions.QueenSide &&
                    Game.GetPiece(3, 0) == null &&
                    Game.GetPiece(2, 0) == null &&
                    Game.GetPiece(1, 0) == null)
                {
                    yield return CreateMoveFromPiece(king, new BoardPosition(2, 0), false);
                }
            }
            else
            {
                if ((Game.BlackCastlingOptions & CastlingOptions.KingSide) == CastlingOptions.KingSide &&
                    Game.GetPiece(5, 7) == null &&
                    Game.GetPiece(6, 7) == null)
                {
                    yield return CreateMoveFromPiece(king, new BoardPosition(6, 7), false);
                }

                if ((Game.BlackCastlingOptions & CastlingOptions.QueenSide) == CastlingOptions.QueenSide &&
                    Game.GetPiece(3, 7) == null &&
                    Game.GetPiece(2, 7) == null &&
                    Game.GetPiece(1, 7) == null)
                {
                    yield return CreateMoveFromPiece(king, new BoardPosition(2, 7), false);
                }
            }

            // Check for normal moves
            foreach (var applyMovement in RoyalMovements)
            {
                var finalPosition = applyMovement(king.Position);
                if (finalPosition.File >= 0 &&
                    finalPosition.File < Game.BoardSize &&
                    finalPosition.Rank >= 0 &&
                    finalPosition.Rank < Game.BoardSize)
                {
                    var pieceAtDestination = Game.GetPiece(finalPosition);
                    if (pieceAtDestination == null)
                    {
                        yield return CreateMoveFromPiece(king, finalPosition, false);
                    }
                    else if (whiteKing != ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType))
                    {
                        yield return CreateMoveFromPiece(king, finalPosition, true);
                    }
                }
            }
        }

        private IEnumerable<Move> GetKnightMoves(ChessPiece knight)
        {
            foreach (var applyMovement in KnightMovements)
            {
                var finalPosition = applyMovement(knight.Position);
                if (finalPosition.File >= 0 &&
                    finalPosition.File < Game.BoardSize &&
                    finalPosition.Rank >= 0 &&
                    finalPosition.Rank < Game.BoardSize)
                {
                    var pieceAtDestination = Game.GetPiece(finalPosition);
                    if (pieceAtDestination == null)
                    {
                        yield return CreateMoveFromPiece(knight, finalPosition, false);
                    }
                    else if (ChessFormatter.IsPieceWhite(knight.PieceType) != ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType))
                    {
                        yield return CreateMoveFromPiece(knight, finalPosition, true);
                    }
                }
            }
        }

        private IEnumerable<Move> GetRookMoves(ChessPiece rook) => GetStraightPieceMoves(rook, RookMovements);

        private IEnumerable<Move> GetBishopMoves(ChessPiece rook) => GetStraightPieceMoves(rook, BishopMovements);

        private IEnumerable<Move> GetQueenMoves(ChessPiece rook) => GetStraightPieceMoves(rook, RoyalMovements);

        private IEnumerable<Move> GetStraightPieceMoves(ChessPiece piece, IEnumerable<Func<BoardPosition, BoardPosition>> movements)
        {
            foreach (var applyMovement in movements)
            {
                var finalPosition = applyMovement(piece.Position);

                while (finalPosition.File >= 0 &&
                    finalPosition.File < Game.BoardSize &&
                    finalPosition.Rank >= 0 &&
                    finalPosition.Rank < Game.BoardSize)
                {
                    var pieceAtDestination = Game.GetPiece(finalPosition);
                    if (pieceAtDestination == null)
                    {
                        yield return CreateMoveFromPiece(piece, finalPosition, false);
                    }
                    else if (ChessFormatter.IsPieceWhite(piece.PieceType) != ChessFormatter.IsPieceWhite(pieceAtDestination.PieceType))
                    {
                        yield return CreateMoveFromPiece(piece, finalPosition, true);
                        break;
                    }
                    else
                    {
                        break;
                    }

                    finalPosition = applyMovement(finalPosition);
                }
            }
        }

        private Move CreateMoveFromPiece(ChessPiece piece, BoardPosition finalPosition, bool capture)
        {
            var promoted = (piece.PieceType == ChessPieces.WhitePawn || piece.PieceType == ChessPieces.BlackPawn) && finalPosition.Rank % (Game.BoardSize - 1) == 0;
            return new Move
            {
                PieceMoved = piece.PieceType,
                OriginalPosition = piece.Position,
                FinalPosition = finalPosition,
                Capture = capture,
                PiecePromotedTo = promoted ? (ChessFormatter.IsPieceWhite(piece.PieceType) ? ChessPieces.WhiteQueen : ChessPieces.BlackQueen) : (ChessPieces?)null
            };
        }
    }
}

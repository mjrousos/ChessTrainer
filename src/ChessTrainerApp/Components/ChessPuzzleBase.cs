using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MjrChess.Engine;
using MjrChess.Engine.Models;
using MjrChess.Trainer.Models;
using MjrChess.Trainer.Services;

namespace MjrChess.Trainer.Components
{
    public class ChessPuzzleBase : ComponentBase
    {
        private ChessEngine puzzleEngine = default!;

        [Inject]
        private ILogger<ChessPuzzleBase> Logger { get; set; } = default!;

        [Inject]
        private IPuzzleService PuzzleService { get; set; } = default!;

        [Inject]
        protected ChessEngine PuzzleEngine
        {
            get => puzzleEngine;
            set
            {
                if (puzzleEngine != null)
                {
                    puzzleEngine.Game.OnMove -= HandleMove;
                }

                puzzleEngine = value;
                puzzleEngine.Game.OnMove += HandleMove;
            }
        }

        protected TacticsPuzzle? CurrentPuzzle { get; set; }

        protected PuzzleState CurrentPuzzleState { get; set; }

        protected async Task LoadNextPuzzleAsync()
        {
            CurrentPuzzle = await PuzzleService.GetPuzzleAsync();
            CurrentPuzzleState = PuzzleState.Ongoing;
            PuzzleEngine.LoadPosition(CurrentPuzzle.Position);
            Logger.LogInformation("Loaded puzzle ID {PuzzleId}", CurrentPuzzle.Id);
            StateHasChanged();
        }

        protected void ResetPuzzle()
        {
            if (CurrentPuzzle != null)
            {
                // Currently it's a no-op to reset an ongoing puzzle (since an ongoing
                // puzzle is already in the starting state), but I'm allowing it since,
                // in the future, multi-step puzzles might make sense to reset while still
                // ongoing.
                CurrentPuzzleState = PuzzleState.Ongoing;
                PuzzleEngine.LoadPosition(CurrentPuzzle.Position);
                Logger.LogInformation("Reset puzzle ID {PuzzleId}", CurrentPuzzle.Id);
                StateHasChanged();
            }
        }

        protected void RevealPuzzle()
        {
            // Reveal is a no-op if there's no puzzle or the puzzle is already revealed or solved
            if (CurrentPuzzle != null && CurrentPuzzleState != PuzzleState.Revealed && CurrentPuzzleState != PuzzleState.Solved)
            {
                if (CurrentPuzzleState == PuzzleState.Missed)
                {
                    ResetPuzzle();
                }

                // Puzzle solutions only include where the piece should move.
                // They don't include information about check, checkmate, etc.
                // By finding the solution move with the current engine, that
                // information is added.
                var pieceMoved = new ChessPiece(CurrentPuzzle.PieceMoved, CurrentPuzzle.Solution.OriginalPosition);
                var solution = puzzleEngine.GetLegalMoves(pieceMoved).SingleOrDefault(m => m.FinalPosition == CurrentPuzzle.Solution.FinalPosition);
                if (solution == null)
                {
                    Logger.LogError("Invalid puzzle {PuzzleId} has impossible solution {Solution}", CurrentPuzzle.Id, CurrentPuzzle.Solution);
                    throw new InvalidOperationException($"Invalid puzzle {CurrentPuzzle.Id} has impossible solution");
                }

                puzzleEngine.Game.Move(solution);
                Logger.LogInformation("Revealed puzzle ID {PuzzleId} solution", CurrentPuzzle.Id);
                CurrentPuzzleState = PuzzleState.Revealed;
                StateHasChanged();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Load initial puzzle in OnAfterRenderAsync instead of in OnInitializedAsync
            // because the latter is invoked twice (once during pre-rendering and once
            // after the client has connected to the server). Therefore, only stable,
            // auth-agnostic initialization should happen there. This method may return different
            // puzzles when called twice resulting in the puzzle the user sees changing
            // after the client-server connection is established. Therefore, load the initial
            // puzzle here instead.
            //
            // Pre-rendering docs and OnInitialized interactions:
            // https://docs.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-3.1#stateful-reconnection-after-prerendering
            // Recommendation to use OnAfterRender for this scenario:
            // https://github.com/dotnet/aspnetcore/issues/13711
            if (firstRender)
            {
                await LoadNextPuzzleAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private void HandleMove(ChessGame game, Move move)
        {
            if (CurrentPuzzle != null)
            {
                if (move == CurrentPuzzle.Solution)
                {
                    Logger.LogInformation("Puzzle {PuzzleId} solved", CurrentPuzzle.Id);
                    CurrentPuzzleState = PuzzleState.Solved;
                    StateHasChanged();
                }
                else
                {
                    Logger.LogInformation("Puzzle {PuzzleId} missed", CurrentPuzzle.Id);
                    CurrentPuzzleState = PuzzleState.Missed;
                    StateHasChanged();
                }
            }
        }
    }
}

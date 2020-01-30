using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
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
        private IJSRuntime JSRuntime { get; set; } = default!;

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

        private async Task LoadNextPuzzleAsync()
        {
            CurrentPuzzle = await PuzzleService.GetPuzzleAsync();
            CurrentPuzzleState = PuzzleState.Ongoing;
            PuzzleEngine.LoadPosition(CurrentPuzzle.Position);
            Logger.LogInformation("Loaded puzzle ID {PuzzleId}", CurrentPuzzle.Id);
            StateHasChanged();
        }

        private void ResetPuzzle()
        {
            if (CurrentPuzzle != null)
            {
                CurrentPuzzleState = PuzzleState.Ongoing;
                PuzzleEngine.LoadPosition(CurrentPuzzle.Position);
                Logger.LogInformation("Reset puzzle ID {PuzzleId}", CurrentPuzzle.Id);
                StateHasChanged();
            }
        }

        private void RevealPuzzle()
        {
            if (CurrentPuzzle != null)
            {
                Logger.LogInformation("Revealed puzzle ID {PuzzleId} solution", CurrentPuzzle.Id);
                CurrentPuzzleState = PuzzleState.Revealed;
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

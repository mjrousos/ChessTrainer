using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MjrChess.Engine;
using MjrChess.Trainer.Models;
using MjrChess.Trainer.Services;

namespace MjrChess.Trainer.Components
{
    public class ChessPuzzleBase : ComponentBase
    {
        [Inject]
        private IPuzzleService PuzzleService { get; set; } = default!;

        [Inject]
        protected ChessEngine PuzzleEngine { get; set; } = default!;

        protected TacticsPuzzle? CurrentPuzzle { get; set; }

        private async Task LoadNextPuzzleAsync()
        {
            CurrentPuzzle = await PuzzleService.GetPuzzleAsync();
            PuzzleEngine.LoadPosition(CurrentPuzzle.Position);
            StateHasChanged();
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
    }
}

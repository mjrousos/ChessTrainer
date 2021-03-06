﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MjrChess.Engine;
using MjrChess.Engine.Models;
using MjrChess.Trainer.Models;
using MjrChess.Trainer.Services;

namespace MjrChess.Trainer.Components
{
    public class ChessPuzzleBase : OwningComponentBase
    {
        private const int HistoryCount = 10;
        private ChessEngine _puzzleEngine = default!;

        [Inject]
        private ILogger<ChessPuzzleBase> Logger { get; set; } = default!;

        private IPuzzleService PuzzleService { get; set; } = default!;

        private IUserService UserService { get; set; } = default!;

        [CascadingParameter]
#pragma warning disable SA1300 // Element should begin with upper-case letter
        private Task<AuthenticationState> authenticationStateTask { get; set; } = default!;
#pragma warning restore SA1300 // Element should begin with upper-case letter

        private async Task<string?> GetUserId() => (await authenticationStateTask)?.User?.GetUserId();

        [Inject]
        protected ChessEngine PuzzleEngine
        {
            get => _puzzleEngine;
            set
            {
                if (_puzzleEngine != null)
                {
                    _puzzleEngine.Game.OnMove -= HandleMove;
                }

                _puzzleEngine = value;
                _puzzleEngine.Game.OnMove += HandleMove;
            }
        }

        private TacticsPuzzle? _currentPuzzle;

        protected TacticsPuzzle? CurrentPuzzle
        {
            get => _currentPuzzle;
            set
            {
                _currentPuzzle = value;
                if (value != null)
                {
                    PuzzleReady = false;
                    PuzzleEngine.LoadFEN(value.Position);
                    MakeMove(value.SetupMove);
                    PuzzleEngine.Game.WhitePlayer = value.WhitePlayerName ?? "White Player";
                    PuzzleEngine.Game.BlackPlayer = value.BlackPlayerName ?? "Black Player";
                    PuzzleReady = true;
                }
            }
        }

        protected PuzzleState CurrentPuzzleState { get; set; }

        protected IList<PuzzleHistory>? PuzzleHistory { get; set; }

        private bool PuzzleReady { get; set; }

        protected bool FirstAttempt { get; set; }

        protected override void OnInitialized()
        {
            PuzzleService = ScopedServices.GetRequiredService<IPuzzleService>();
            UserService = ScopedServices.GetRequiredService<IUserService>();

            base.OnInitialized();
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
                var userId = await GetUserId();
                PuzzleHistory = userId is null
                    ? new List<PuzzleHistory>()
                    : (await UserService.GetPuzzleHistoryAsync(userId))?.OrderByDescending(h => h.LastModifiedDate).Take(HistoryCount).ToList() ?? new List<PuzzleHistory>();
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected async Task LoadNextPuzzleAsync() => LoadPuzzle(await PuzzleService.GetRandomPuzzleAsync(await GetUserId()));

        protected async Task LoadPuzzleByIdAsync(int id) => LoadPuzzle(await PuzzleService.GetPuzzleAsync(id));

        private void LoadPuzzle(TacticsPuzzle? puzzle)
        {
            if (puzzle != null)
            {
                CurrentPuzzle = puzzle;
                CurrentPuzzleState = PuzzleState.Ongoing;
                FirstAttempt = true;
                Logger.LogInformation("Loaded puzzle ID {PuzzleId}", CurrentPuzzle?.Id);
                StateHasChanged();
            }
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
                CurrentPuzzle = CurrentPuzzle;
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

                // Users don't get credit for solving (or missing) a puzzle once they know the solution
                FirstAttempt = false;

                MakeMove(CurrentPuzzle.Solution);

                Logger.LogInformation("Revealed puzzle ID {PuzzleId} solution", CurrentPuzzle.Id);
                CurrentPuzzleState = PuzzleState.Revealed;
                StateHasChanged();
            }
        }

        private async void HandleMove(ChessGame game, Move move)
        {
            var userId = await GetUserId();
            if (CurrentPuzzle != null && PuzzleReady)
            {
                if (move == CurrentPuzzle.Solution)
                {
                    Logger.LogInformation("Puzzle {PuzzleId} solved by {UserId}", CurrentPuzzle.Id, userId ?? "Anonymous");
                    CurrentPuzzleState = PuzzleState.Solved;
                }
                else
                {
                    Logger.LogInformation("Puzzle {PuzzleId} missed by {UserId}", CurrentPuzzle.Id, userId ?? "Anonymous");
                    CurrentPuzzleState = PuzzleState.Missed;
                }

                // Record the user's first attempt in puzzle history
                if (FirstAttempt)
                {
                    var puzzleHistory = new PuzzleHistory
                    {
                        UserId = userId,
                        Puzzle = CurrentPuzzle,
                        Solved = CurrentPuzzleState == PuzzleState.Solved
                    };

                    PuzzleHistory?.Insert(0, puzzleHistory);
                    while (PuzzleHistory?.Count > HistoryCount)
                    {
                        PuzzleHistory.RemoveAt(HistoryCount);
                    }

                    if (!(userId is null))
                    {
                        await UserService.RecordPuzzleHistoryAsync(puzzleHistory);
                    }

                    FirstAttempt = false;
                }

                StateHasChanged();
            }
        }

        private void MakeMove(Move move)
        {
            // Moves in puzzles only include where the piece should move.
            // They don't include information about check, checkmate, etc.
            // By finding the move with the current engine, that information
            // is added (so that it will display correctly).
            if (CurrentPuzzle is null)
            {
                return;
            }

            var resolvedMove = PuzzleEngine.GetLegalMoves(move.OriginalPosition).SingleOrDefault(m => m.FinalPosition == move.FinalPosition);
            if (resolvedMove == null)
            {
                Logger.LogError("Invalid puzzle move ({Move}) for {PuzzleId}", move, CurrentPuzzle.Id);
                throw new InvalidOperationException($"Invalid move ({move}) for puzzle {CurrentPuzzle.Id}");
            }

            PuzzleEngine.Game.Move(resolvedMove);
        }
    }
}

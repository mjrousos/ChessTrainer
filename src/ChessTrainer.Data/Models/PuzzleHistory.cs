﻿namespace MjrChess.Trainer.Data.Models
{
    public class PuzzleHistory : EntityBase
    {
        public string UserId { get; set; } = default!;

        public TacticsPuzzle Puzzle { get; set; } = default!;

        public bool Solved { get; set; }
    }
}

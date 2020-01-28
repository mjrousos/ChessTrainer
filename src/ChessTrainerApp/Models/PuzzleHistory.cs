namespace MjrChess.Trainer.Models
{
    public class PuzzleHistory : IEntity
    {
        public string UserId { get; set; } = default!;

        public TacticsPuzzle Puzzle { get; set; } = default!;

        public bool Solved { get; set; }
    }
}

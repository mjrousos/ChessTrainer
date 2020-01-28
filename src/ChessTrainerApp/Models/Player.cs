namespace MjrChess.Trainer.Models
{
    public class Player : EntityBase
    {
        public Player(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new System.ArgumentException("message", nameof(name));
            }

            Name = name;
        }

        public string Name { get; set; }

        public ChessSites Site { get; set; } = ChessSites.Other;
    }
}

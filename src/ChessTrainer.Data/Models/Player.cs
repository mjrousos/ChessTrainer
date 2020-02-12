using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data.Models
{
    public class Player : IEntity
    {
        public Player(string name, ChessSites site)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new System.ArgumentException("message", nameof(name));
            }

            Name = name;
            Site = site;
        }

        public string Name { get; set; }

        public ChessSites Site { get; set; }
    }
}

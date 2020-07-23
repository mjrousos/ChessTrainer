using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data.Models
{
    /// <summary>
    /// A chess player who played games puzzles were generated from.
    /// </summary>
    public class Player : EntityBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        /// <param name="name">The player's username.</param>
        /// <param name="site">The chess site the player plays on.</param>
        public Player(string name, ChessSites site)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new System.ArgumentException("message", nameof(name));
            }

            Name = name;
            Site = site;
        }

        /// <summary>
        /// Gets or sets player's username.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the site the user plays on.
        /// </summary>
        public ChessSites Site { get; set; }
    }
}

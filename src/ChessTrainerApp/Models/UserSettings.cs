using System.Collections.Generic;

namespace MjrChess.Trainer.Models
{
    public class UserSettings : EntityBase
    {
        public ICollection<Player> PreferredPlayers { get; set; } = new HashSet<Player>();
    }
}

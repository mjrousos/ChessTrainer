using System.Collections.Generic;

namespace MjrChess.Trainer.Models
{
    public class UserSettings : IEntity
    {
        public string UserId { get; set; } = default!;

        public ICollection<UserSettingsXPlayer> PreferredPlayers { get; set; } = new HashSet<UserSettingsXPlayer>();
    }
}

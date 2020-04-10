using System.Collections.Generic;

namespace MjrChess.Trainer.Data.Models
{
    public class UserSettings : EntityBase
    {
        public string UserId { get; set; } = default!;

        public ICollection<UserSettingsXPlayer> PreferredPlayers { get; set; } = new HashSet<UserSettingsXPlayer>();
    }
}

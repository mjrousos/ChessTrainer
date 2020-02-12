namespace MjrChess.Trainer.Models
{
    public class UserSettingsXPlayer
    {
        public int UserSettingsId { get; set; }

        public int PlayerId { get; set; }

        public UserSettings UserSettings { get; set; } = default!;

        public Player Player { get; set; } = default!;
    }
}

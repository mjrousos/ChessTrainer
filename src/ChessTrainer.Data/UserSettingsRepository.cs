using AutoMapper;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class UserSettingsRepository : EFRepository<Data.Models.UserSettings, UserSettings>
    {
        public UserSettingsRepository(PuzzleDbContext context, IMapper mapper, ILogger<EFRepository<Data.Models.UserSettings, UserSettings>> logger)
            : base(context, mapper, logger)
        { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class UserSettingsRepository : EFRepository<UserSettings>
    {
        public UserSettingsRepository(PuzzleDbContext context, ILogger<EFRepository<UserSettings>> logger)
            : base(context, logger)
        { }

        protected override IQueryable<UserSettings> DbSetWithRelatedEntities =>
            base.DbSetWithRelatedEntities.Include(s => s.PreferredPlayers).ThenInclude(p => p.Player);
    }
}

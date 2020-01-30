using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class TacticsPuzzleRepository : EFRepository<TacticsPuzzle>
    {
        public TacticsPuzzleRepository(PuzzleDbContext context, ILogger<EFRepository<TacticsPuzzle>> logger)
            : base(context, logger)
        {
        }

        protected override IQueryable<TacticsPuzzle> DbSetWithRelatedEntities =>
            base.DbSetWithRelatedEntities.Include(p => p.WhitePlayer).Include(p => p.BlackPlayer);
    }
}

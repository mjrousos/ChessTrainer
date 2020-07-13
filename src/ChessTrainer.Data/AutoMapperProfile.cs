using System;
using AutoMapper;

namespace MjrChess.Trainer.Data
{
    public class AutoMapperProfile : Profile
    {
        public PuzzleDbContext Context { get; }

        public AutoMapperProfile(PuzzleDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));

            CreateMap<Data.Models.Player, Trainer.Models.Player>();
            CreateMap<Data.Models.PuzzleHistory, Trainer.Models.PuzzleHistory>();
            CreateMap<Data.Models.TacticsPuzzle, Trainer.Models.TacticsPuzzle>();
            CreateMap<Data.Models.UserSettings, Trainer.Models.UserSettings>();
            CreateMap<Trainer.Models.Player, Data.Models.Player>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate(p, new Data.Models.Player(p.Name, p.Site)));
            CreateMap<Trainer.Models.PuzzleHistory, Data.Models.PuzzleHistory>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate(p, new Data.Models.PuzzleHistory()));
            CreateMap<Trainer.Models.TacticsPuzzle, Data.Models.TacticsPuzzle>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate(p, new Data.Models.TacticsPuzzle(p.Position)));
            CreateMap<Trainer.Models.UserSettings, Data.Models.UserSettings>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate(p, new Data.Models.UserSettings()));
        }

        private TDomainType GetDbObjectOrCreate<TDtoType, TDomainType>(TDtoType dto, TDomainType defaultObject)
            where TDtoType : Trainer.Models.IEntity
            where TDomainType : Data.Models.EntityBase
        {
            TDomainType? ret = null;
            if (dto.Id != 0)
            {
                ret = Context.Find<TDomainType>(dto.Id);
            }

            return ret ?? defaultObject;
        }
    }
}

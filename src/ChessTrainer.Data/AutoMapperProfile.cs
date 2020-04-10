using System;
using System.Collections.Generic;
using System.Linq;
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
            CreateMap<Data.Models.UserSettings, Trainer.Models.UserSettings>()
                .ForMember(s => s.PreferredPlayers, opt => opt.MapFrom(s => s.PreferredPlayers.Select(x => new Trainer.Models.Player(x.Player.Name, x.Player.Site)
                {
                    Id = x.Player.Id,
                    CreatedDate = x.Player.CreatedDate,
                    LastModifiedDate = x.Player.LastModifiedDate
                })));

            CreateMap<Trainer.Models.Player, Data.Models.Player>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate<Trainer.Models.Player, Models.Player>(p, new Data.Models.Player(p.Name, p.Site)));
            CreateMap<Trainer.Models.PuzzleHistory, Data.Models.PuzzleHistory>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate<Trainer.Models.PuzzleHistory, Models.PuzzleHistory>(p, new Data.Models.PuzzleHistory()));
            CreateMap<Trainer.Models.TacticsPuzzle, Data.Models.TacticsPuzzle>()
                .ConstructUsing((p, resolutonContext) => GetDbObjectOrCreate<Trainer.Models.TacticsPuzzle, Models.TacticsPuzzle>(p, new Data.Models.TacticsPuzzle(p.Position)));
            CreateMap<Trainer.Models.UserSettings, Data.Models.UserSettings>()
                .ForMember(s => s.PreferredPlayers, opt => opt.MapFrom(GetPreferredPlayers));
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

        private ICollection<Data.Models.UserSettingsXPlayer> GetPreferredPlayers(Trainer.Models.UserSettings source, Data.Models.UserSettings dest)
        {
            // TODO : Optimize for perf? Or is this not hot enough to matter?
            var settings = new HashSet<Data.Models.UserSettingsXPlayer>();
            foreach (var player in source.PreferredPlayers)
            {
                var x = Context.UserSettingsXPlayers.Find(source.Id, player.Id);

                if (x is null)
                {
                    x = Context.UserSettingsXPlayers.Add(new Data.Models.UserSettingsXPlayer { UserSettingsId = source.Id, PlayerId = player.Id }).Entity;
                }

                Context.Entry(x).Reference(x => x.Player).Load();
                Context.Entry(x).Reference(x => x.UserSettings).Load();

                settings.Add(x);
            }

            return settings;
        }
    }
}

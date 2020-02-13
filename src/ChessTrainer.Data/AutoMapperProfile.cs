using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class AutoMapperProfile : Profile
    {
        public PuzzleDbContext Context { get; }

        public AutoMapperProfile(PuzzleDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));

            CreateMap<Data.Models.Player, Player>();
            CreateMap<Data.Models.PuzzleHistory, PuzzleHistory>();
            CreateMap<Data.Models.TacticsPuzzle, TacticsPuzzle>();
            CreateMap<Data.Models.UserSettings, UserSettings>()
                .ForMember(s => s.PreferredPlayers, opt => opt.MapFrom(s => s.PreferredPlayers.Select(x => new Player(x.Player.Name, x.Player.Site)
                {
                    Id = x.Player.Id,
                    CreatedDate = x.Player.CreatedDate,
                    LastModifiedDate = x.Player.LastModifiedDate
                })));

            CreateMap<Player, Data.Models.Player>();
            CreateMap<PuzzleHistory, Data.Models.PuzzleHistory>();
            CreateMap<TacticsPuzzle, Data.Models.TacticsPuzzle>();
            CreateMap<UserSettings, Data.Models.UserSettings>()
                .ForMember(s => s.PreferredPlayers, opt => opt.MapFrom(GetPreferredPlayers));
        }

        private ICollection<Data.Models.UserSettingsXPlayer> GetPreferredPlayers(UserSettings source, Data.Models.UserSettings dest)
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

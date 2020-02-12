using AutoMapper;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Data.Models.Player, Player>();
            CreateMap<Data.Models.PuzzleHistory, PuzzleHistory>();
            CreateMap<Data.Models.TacticsPuzzle, TacticsPuzzle>();
            CreateMap<Data.Models.UserSettings, UserSettings>();
            CreateMap<Data.Models.UserSettingsXPlayer, UserSettingsXPlayer>();

            CreateMap<Player, Data.Models.Player>();
            CreateMap<PuzzleHistory, Data.Models.PuzzleHistory>();
            CreateMap<TacticsPuzzle, Data.Models.TacticsPuzzle>();
            CreateMap<UserSettings, Data.Models.UserSettings>();
            CreateMap<UserSettingsXPlayer, Data.Models.UserSettingsXPlayer>();
        }
    }
}

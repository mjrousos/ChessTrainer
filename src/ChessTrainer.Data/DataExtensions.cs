using System;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public static class DataExtensions
    {
        public static IServiceCollection AddChessTrainerData(this IServiceCollection services, string dbConnectionString)
        {
            services.AddScoped<IMapper>(provider => new MapperConfiguration(
                cfg =>
                {
                    cfg.AddExpressionMapping();
                    cfg.AddProfile(new AutoMapperProfile(provider.GetRequiredService<PuzzleDbContext>()));
                },
                provider.GetRequiredService<ILoggerFactory>()).CreateMapper());

            services.AddScoped<IRepository<Player>, EFRepository<Data.Models.Player, Player>>();
            services.AddScoped<IRepository<PuzzleHistory>, EFRepository<Data.Models.PuzzleHistory, PuzzleHistory>>();
            services.AddScoped<IRepository<TacticsPuzzle>, TacticsPuzzleRepository>();
            services.AddScoped<IRepository<UserSettings>, UserSettingsRepository>();

            services.AddDbContext<PuzzleDbContext>(options =>
                options.UseSqlServer(dbConnectionString, options =>
                {
                    options.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    options.MigrationsAssembly("MjrChess.Trainer"); // TODO : Move migrations to the ChessTrainer.Data project
                }));

            return services;
        }
    }
}

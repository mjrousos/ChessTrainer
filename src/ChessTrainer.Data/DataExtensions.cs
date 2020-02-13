using System;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public static class DataExtensions
    {
        public static IServiceCollection AddChessTrainerData(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfile(provider.GetRequiredService<PuzzleDbContext>()));
            }).CreateMapper());

            services.AddScoped<IRepository<Player>, EFRepository<Data.Models.Player, Player>>();
            services.AddScoped<IRepository<PuzzleHistory>, EFRepository<Data.Models.PuzzleHistory, PuzzleHistory>>();
            services.AddScoped<IRepository<TacticsPuzzle>, TacticsPuzzleRepository>();
            services.AddScoped<IRepository<UserSettings>, UserSettingsRepository>();

            services.AddDbContext<PuzzleDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("PuzzleDatabase"), options =>
                {
                    options.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                }));

            return services;
        }
    }
}

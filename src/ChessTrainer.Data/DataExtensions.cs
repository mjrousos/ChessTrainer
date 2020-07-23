﻿using System;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public static class DataExtensions
    {
        /// <summary>
        /// Register ChessTrainer.Data services with an IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to register services with.</param>
        /// <param name="dbConnectionString">The connection string for the SQL database in use.</param>
        /// <returns>The IServiceCollection after registering data services.</returns>
        public static IServiceCollection AddChessTrainerData(this IServiceCollection services, string dbConnectionString)
        {
            // Register automapper services
            services.AddScoped(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfile(provider.GetRequiredService<PuzzleDbContext>()));
            }).CreateMapper());

            // Register EF repositories
            services.AddScoped<IRepository<Player>, EFRepository<Data.Models.Player, Player>>();
            services.AddScoped<IRepository<PuzzleHistory>, EFRepository<Data.Models.PuzzleHistory, PuzzleHistory>>();
            services.AddScoped<IRepository<TacticsPuzzle>, TacticsPuzzleRepository>();

            // Register EF.Core context
            services.AddDbContext<PuzzleDbContext>(options =>
                options.UseSqlServer(dbConnectionString, options =>
                {
                    options.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                }));

            return services;
        }
    }
}

﻿using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MjrChess.Engine;
using MjrChess.Trainer.Data;
using MjrChess.Trainer.Services;

namespace MjrChess.Trainer
{
    public class Startup
    {
        private const string EnableCompressionKey = "EnableResponseCompression";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.Bind("AzureAdB2C", options));

            services.AddChessTrainerData(Configuration.GetConnectionString("PuzzleDatabase"));

            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IPuzzleService, PuzzleService>();
            services.AddScoped<IUserService, UserService>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes;
            });
            services.AddHealthChecks();

            services.AddTransient<ChessEngine>();
            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, TelemetryConfiguration aiConfig)
        {
            // Issue: https://github.com/dotnet/aspnetcore/issues/18865
            app.UseRewriter(new RewriteOptions().AddRedirect("AzureADB2C/Account/SignedOut", "/"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                aiConfig.DisableTelemetry = true;
            }
            else
            {
                app.UseExceptionHandler("/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            if (bool.TryParse(Configuration[EnableCompressionKey], out var useResponseCompression) && useResponseCompression)
            {
                app.UseResponseCompression();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapHealthChecks("/hc");
                endpoints.MapControllers(); // For signin/signout endpoints
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}

using System;
using Azure.Storage.Queues;
using HealthChecks.Azure.Storage.Queues;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"));

            services.AddChessTrainerData(Configuration.GetConnectionString("PuzzleDatabase")
                ?? throw new InvalidOperationException("ConnectionStrings:PuzzleDatabase configuration value is required."));

            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IPuzzleService, PuzzleService>();
            services.AddScoped<IUserService, UserService>();

            services.AddRazorPages()
                .AddMicrosoftIdentityUI();
            services.AddServerSideBlazor();
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes;
            });
            var healthChecks = services.AddHealthChecks()
                .AddDbContextCheck<PuzzleDbContext>(tags: new[] { "ready" });

            // The companion IngestionFunctions project writes games to this queue; if it is
            // unavailable ChessTrainerApp cannot trigger game ingestion.  The check is opt-in:
            // supply StorageConnectionString in configuration to enable it.
            var storageConnectionString = Configuration["StorageConnectionString"];
            if (!string.IsNullOrWhiteSpace(storageConnectionString))
            {
                var gameQueueName = Configuration["GameIngestionQueue"] ?? "games";
                healthChecks.AddAzureQueueStorage(
                    clientFactory: _ => new QueueServiceClient(storageConnectionString),
                    optionsFactory: _ => new AzureQueueStorageHealthCheckOptions { QueueName = gameQueueName },
                    name: "azure-storage-queue",
                    failureStatus: null,
                    tags: new[] { "ready" },
                    timeout: null);
            }

            services.AddTransient<ChessEngine>();

            // Application Insights 3.x throws at startup if no connection string or
            // instrumentation key is configured. The original 2.x code passed an empty
            // instrumentation key, which the SDK silently treated as "no telemetry";
            // preserve that behavior by only registering telemetry when configured.
            var aiConnectionString = Configuration["ApplicationInsights:ConnectionString"];
            var aiInstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            if (!string.IsNullOrWhiteSpace(aiConnectionString) || !string.IsNullOrWhiteSpace(aiInstrumentationKey))
            {
                services.AddApplicationInsightsTelemetry();
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Redirect post-signout requests from the Microsoft.Identity.Web UI to the site root.
            app.UseRewriter(new RewriteOptions().AddRedirect("MicrosoftIdentity/Account/SignedOut", "/"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // TelemetryConfiguration is only registered when AddApplicationInsightsTelemetry()
                // was called above; use GetService (not GetRequiredService) to avoid throwing
                // when telemetry is intentionally disabled via missing configuration.
                var aiConfig = app.ApplicationServices.GetService<TelemetryConfiguration>();
                if (aiConfig is not null)
                {
                    aiConfig.DisableTelemetry = true;
                }
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

                // Liveness probe — returns 200 as long as the process is responsive.
                // Predicate = _ => false intentionally skips all registered checks (including the DB
                // check) so this endpoint is zero-cost and safe for App Service / Kubernetes liveness probes.
                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false })
                    .AllowAnonymous();

                // Readiness probe — returns 200 only when external dependencies (DB, config) are healthy.
                // Used by load balancers / readiness probes to gate traffic.
                endpoints.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
                    .AllowAnonymous();

                endpoints.MapControllers(); // For signin/signout endpoints
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}

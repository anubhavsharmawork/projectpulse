using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Runs all database seeders as a background task after the host starts.
    /// This ensures Kestrel binds to $PORT immediately (critical for Heroku's
    /// 60-second boot-timeout) while seeding continues in the background.
    /// Migrations still run synchronously in Program.cs before the host starts.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class DatabaseSeederHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DatabaseSeederHostedService> _logger;

        public DatabaseSeederHostedService(
            IServiceProvider services,
            ILogger<DatabaseSeederHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Yield immediately so the host startup pipeline is not blocked.
            await Task.Yield();

            _logger.LogInformation("Background database seeding started.");

            try
            {
                // Quick readiness check — abort if the schema is not there yet.
                using (var probe = _services.CreateScope())
                {
                    var db = probe.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (!await db.Database.CanConnectAsync(stoppingToken))
                    {
                        _logger.LogWarning("Database not reachable — skipping background seeding.");
                        return;
                    }
                }

                await RunSeedersAsync(stoppingToken);

                _logger.LogInformation("Background database seeding completed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Background seeding cancelled during shutdown.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background database seeding failed — app continues without seed data.");
            }
        }

        private async Task RunSeedersAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await SafeSeedAsync("Demo users", () => _services.SeedDemoAsync(_logger), ct);
            await SafeSeedAsync("Roles & permissions", () => _services.SeedRolesAndPermissionsAsync(_logger), ct);
            await SafeSeedAsync("User→AppRole linking", () => _services.LinkUsersToAppRolesAsync(_logger), ct);
            await SafeSeedAsync("Domain templates", () => _services.SeedDomainTemplatesAsync(_logger), ct);
            await SafeSeedAsync("Workflows", () => _services.SeedWorkflowsAsync(_logger), ct);
            await SafeSeedAsync("Project categories", () => _services.SeedProjectCategoriesAsync(_logger), ct);
            await SafeSeedAsync("Demo bugs", () => _services.SeedDemoBugsAsync(_logger), ct);
            await SafeSeedAsync("Domain asset configs", () => _services.SeedDomainAssetConfigsAsync(_logger), ct);
            await SafeSeedAsync("Legal documents", () => _services.SeedLegalDocumentsAsync(_logger), ct);
        }

        private async Task SafeSeedAsync(string name, Func<Task> seeder, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await seeder();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{SeederName} seeding failed, continuing.", name);
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence
{
    [ExcludeFromCodeCoverage]
    public static class DemoBugSeeder
    {
        public static async Task SeedDemoBugsAsync(this IServiceProvider services, ILogger logger)
        {
            logger.LogInformation("Bug seeding skipped — create bugs manually via the UI to test dashboard metrics.");
            await Task.CompletedTask;
        }
    }
}

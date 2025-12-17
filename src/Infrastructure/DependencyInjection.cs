using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Npgsql timestamp behavior for consistency
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            services.AddDbContext<AppDbContext>(options =>
            {
                // Use Heroku DATABASE_URL if available, otherwise fall back to connection string
                var databaseUrl = configuration["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");
                string? conn = null;
                if (!string.IsNullOrWhiteSpace(databaseUrl))
                {
                    conn = ConvertDatabaseUrlToNpgsql(databaseUrl!);
                }
                else
                {
                    conn = configuration.GetConnectionString("Default");
                }

                options.UseNpgsql(conn, npgsql =>
                {
                    // Set migrations assembly to Infrastructure project
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                });
            });

            // Register application database context interface
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Register JWT and storage services
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IStorageService, S3StorageService>();

            return services;
        }

        private static string ConvertDatabaseUrlToNpgsql(string databaseUrl)
        {
            // Parse Heroku-style postgres:// URL
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.Trim('/');

            // Build Npgsql connection string with SSL enabled
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }
    }
}

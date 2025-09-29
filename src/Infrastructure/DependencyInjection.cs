using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Ensure consistent timestamp behavior on Npgsql across environments
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            services.AddDbContext<AppDbContext>(options =>
            {
                // Prefer Heroku DATABASE_URL if present
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
                    // Explicitly set the migrations assembly to the Infrastructure project where migrations live
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                });
            });

            // Expose EF context through the application-layer interface
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // JWT and Storage services
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IStorageService, S3StorageService>();

            return services;
        }

        private static string ConvertDatabaseUrlToNpgsql(string databaseUrl)
        {
            // Expected format: postgres://username:password@host:port/database
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.Trim('/');

            // Enforce SSL for Heroku
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }
    }
}

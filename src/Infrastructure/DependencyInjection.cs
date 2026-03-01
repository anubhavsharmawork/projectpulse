using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Services;
using Infrastructure.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            // Register tenant service (must be before DbContext since AppDbContext depends on it)
            services.AddScoped<ITenantService, TenantService>();

            // Register field-level encryption service (singleton — key is immutable per deployment)
            services.AddSingleton<IEncryptionService, AesFieldEncryptionService>();

            // Register audit interceptor (needs IHttpContextAccessor, so register as scoped)
            services.AddScoped<AuditInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var conn = ResolveConnectionString(configuration);

                options.UseNpgsql(conn, npgsql =>
                {
                    // Set migrations assembly to Infrastructure project
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                });

                // Add audit interceptor
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            // Register application database context interface
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Register generic repository and unit of work
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register JWT and storage services
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IStorageService, S3StorageService>();

            // Register workflow engine
            services.AddScoped<IWorkflowEngine, WorkflowEngine>();

            // Register feedback processor
            services.AddScoped<IFeedbackProcessor, FeedbackProcessor>();

            // Database seeders run as a background service so that Kestrel binds to
            // $PORT immediately and Heroku's boot-timeout is never exceeded.
            services.AddHostedService<DatabaseSeederHostedService>();

            return services;
        }

        /// <summary>
        /// Resolve the Npgsql connection string from Heroku DATABASE_URL or appsettings.
        /// </summary>
        public static string? ResolveConnectionString(IConfiguration configuration)
        {
            var databaseUrl = configuration["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrWhiteSpace(databaseUrl))
                return ConvertDatabaseUrlToNpgsql(databaseUrl!);
            return configuration.GetConnectionString("Default");
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

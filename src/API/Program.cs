using API.Hubs;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Application.Auth.Commands; // for MediatR assembly discovery

var builder = WebApplication.CreateBuilder(args);

// Serilog to STDOUT
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Project API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] {} }
    });
});

// MediatR – include Application assembly so handlers are discovered
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Program).Assembly,
    typeof(LoginUserHandler).Assembly));

// Infrastructure (DbContext, S3, JWT)
builder.Services.AddInfrastructure(builder.Configuration);

// IHttpContextAccessor for command handlers
builder.Services.AddHttpContextAccessor();

// CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy
            // We use JWT in Authorization header (no cookies), so credentials are not required
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// SignalR
builder.Services.AddSignalR();

// AuthN/Z
// Use the same fallback as JwtTokenService to avoid signature mismatches when JWT:Key is not configured
const string JwtFallbackKey = "dev-secret-change-me-please-at-least-32-chars";
var configuredKey = builder.Configuration["JWT:Key"];
var jwtKey = string.IsNullOrWhiteSpace(configuredKey) || Encoding.UTF8.GetByteCount(configuredKey) < 32
    ? JwtFallbackKey
    : configuredKey;
var jwtIssuer = builder.Configuration["JWT:Issuer"];
var jwtAudience = builder.Configuration["JWT:Audience"];
var validateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer);
var validateAudience = !string.IsNullOrWhiteSpace(jwtAudience);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = validateIssuer,
            ValidateAudience = validateAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Allow JWT via query for WebSockets/SignalR
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MemberPolicy", policy => policy.RequireRole("Member", "Admin"));
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply schema reset (optional) and migrations with retry to avoid crash on cold start
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        var conn = db.Database.GetDbConnection();
        // Log minimal connection info (no password)
        logger.LogInformation("DB provider: {Provider}; DataSource: {DataSource}; Database: {Db}", conn.GetType().Name, conn.DataSource, conn.Database);
    }
    catch { }

    // Decide destructive ops defaults: true in Production if not explicitly configured
    bool IsTrue(string? v) => !string.IsNullOrWhiteSpace(v) && v.Equals("true", StringComparison.OrdinalIgnoreCase);

    var dropBeforeEnv = Environment.GetEnvironmentVariable("DROP_SCHEMA_BEFORE_MIGRATE");
    var dropBefore = string.IsNullOrWhiteSpace(dropBeforeEnv) ? app.Environment.IsProduction() : IsTrue(dropBeforeEnv);

    if (dropBefore)
    {
        try
        {
            logger.LogWarning("DROP_SCHEMA_BEFORE_MIGRATE={Drop} -> Dropping schema 'public' before migrations", dropBeforeEnv ?? "(default:true)");
            await db.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE;");
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS public;");
            // ensure privileges for current user
            await db.Database.ExecuteSqlRawAsync("GRANT ALL ON SCHEMA public TO public;");
            logger.LogWarning("Schema 'public' recreated. Proceeding to migrations.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to drop/recreate schema before migration");
        }
    }
    else
    {
        logger.LogInformation("DROP_SCHEMA_BEFORE_MIGRATE disabled");
    }

    try
    {
        var applied = await db.Database.GetAppliedMigrationsAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();
        logger.LogInformation("Applied migrations: {AppliedCount}; Pending: {PendingCount}", applied.Count(), pending.Count());
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Unable to query migration state before migrate");
    }

    const int maxAttempts = 8; // try a bit longer on cold starts
    var migrationsSucceeded = false;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migration completed without exception on attempt {Attempt}", attempt);
            // don't mark succeeded yet; verify tables exist below
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed on attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
            var delaySec = Math.Min(60, (int)Math.Pow(2, attempt)); // 2..60s
            await Task.Delay(TimeSpan.FromSeconds(delaySec));
        }
    }

    try
    {
        // If there are still no tables, create them from the current model
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!await creator.HasTablesAsync())
        {
            await creator.CreateTablesAsync();
            logger.LogWarning("CreateTables executed to bootstrap schema (no tables detected).");
        }

        // Final verification that schema exists before seeding
        if (await creator.HasTablesAsync())
        {
            migrationsSucceeded = true;
        }
        else
        {
            logger.LogWarning("No tables detected after migration and fallback. Seeding will be skipped.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "CreateTables fallback or verification failed");
    }

    try
    {
        var applied = await db.Database.GetAppliedMigrationsAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();
        logger.LogInformation("After init: Applied migrations: {AppliedCount}; Pending: {PendingCount}", applied.Count(), pending.Count());
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Unable to query migration state after init");
    }

    // Optional destructive reset AFTER migrations
    var wipeEnv = Environment.GetEnvironmentVariable("WIPE_DB_ON_STARTUP");
    var wipe = string.IsNullOrWhiteSpace(wipeEnv) ? app.Environment.IsProduction() : IsTrue(wipeEnv);

    if (wipe)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("TRUNCATE \"Comments\", \"Tasks\", \"Projects\", \"Users\" RESTART IDENTITY CASCADE;");
            logger.LogWarning("All data wiped due to WIPE_DB_ON_STARTUP={Wipe}. Database has been reset.", wipeEnv ?? "(default:true)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WIPE_DB_ON_STARTUP failed");
        }
    }
    else
    {
        logger.LogInformation("WIPE_DB_ON_STARTUP disabled");
    }

    // Seed demo data (only if schema is present)
    if (migrationsSucceeded)
    {
        try
        {
            await app.Services.SeedDemoAsync(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seeding failed, continuing startup without demo data.");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

// Serve SPA static files from wwwroot (Angular build)
app.UseDefaultFiles();
app.UseStaticFiles();

// Ensure routing runs before CORS so preflight OPTIONS are handled by the CORS middleware
app.UseRouting();
app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ProjectHub>("/hubs/project");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Fallback to index.html for client-side routes
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }

using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Controllers
{
    [ApiController]
    [Route("api/_ops")] // operational endpoints
    public class OpsController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("init")] // POST /api/_ops/init?secret=XYZ
        public async Task<IActionResult> Init([FromServices] AppDbContext db, [FromServices] ILogger<OpsController> logger, [FromQuery] string? secret = null)
        {
            var expected = Environment.GetEnvironmentVariable("INIT_SECRET");
            if (!string.IsNullOrEmpty(expected))
            {
                if (secret != expected) return NotFound(); // hide existence if secret mismatches
            }

            var result = new List<string>();

            try
            {
                var applied = await db.Database.GetAppliedMigrationsAsync();
                var pending = await db.Database.GetPendingMigrationsAsync();
                result.Add($"Before: applied={applied.Count()}, pending={pending.Count()}");
            }
            catch (Exception ex)
            {
                result.Add($"Before: failed to read migrations: {ex.Message}");
            }

            try
            {
                await db.Database.MigrateAsync();
                result.Add("Migrate: ok");
            }
            catch (Exception ex)
            {
                result.Add($"Migrate: error: {ex.Message}");
            }

            try
            {
                var creator = db.GetService<IRelationalDatabaseCreator>();
                if (!await creator.HasTablesAsync())
                {
                    await creator.CreateTablesAsync();
                    result.Add("CreateTables: executed (no tables detected)");
                }
                else
                {
                    result.Add("CreateTables: skipped (tables detected)");
                }
            }
            catch (Exception ex)
            {
                result.Add($"CreateTables: error: {ex.Message}");
            }

            try
            {
                var applied = await db.Database.GetAppliedMigrationsAsync();
                var pending = await db.Database.GetPendingMigrationsAsync();
                result.Add($"After: applied={applied.Count()}, pending={pending.Count()}");
            }
            catch (Exception ex)
            {
                result.Add($"After: failed to read migrations: {ex.Message}");
            }

            return Ok(new { ok = true, steps = result });
        }
    }
}

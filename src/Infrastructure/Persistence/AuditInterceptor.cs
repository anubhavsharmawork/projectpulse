using Domain.Attributes;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Infrastructure.Persistence
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _http;

        // Entity types we skip auditing (AuditLog itself, and high-frequency read models)
        private static readonly HashSet<string> SkippedTypes = new(StringComparer.Ordinal)
        {
            nameof(AuditLog),
            nameof(MentionNotification)
        };

        public AuditInterceptor(IHttpContextAccessor http)
        {
            _http = http;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null) return ValueTask.FromResult(result);

            var userId = GetCurrentUserId();
            var entries = eventData.Context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => !SkippedTypes.Contains(e.Entity.GetType().Name))
                .ToList();

            foreach (var entry in entries)
            {
                var entityType = entry.Entity.GetType().Name;
                var entityId = GetEntityId(entry);
                var action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                };

                string? oldValues = null;
                string? newValues = null;

                if (entry.State == EntityState.Modified)
                {
                    var changed = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => RedactIfEncrypted(p));
                    var current = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => RedactIfEncrypted(p, useCurrent: true));
                    oldValues = JsonSerializer.Serialize(changed);
                    newValues = JsonSerializer.Serialize(current);
                }
                else if (entry.State == EntityState.Added)
                {
                    var props = entry.Properties
                        .ToDictionary(p => p.Metadata.Name, p => RedactIfEncrypted(p, useCurrent: true));
                    newValues = JsonSerializer.Serialize(props);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var props = entry.Properties
                        .ToDictionary(p => p.Metadata.Name, p => RedactIfEncrypted(p));
                    oldValues = JsonSerializer.Serialize(props);
                }

                eventData.Context.Set<AuditLog>().Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    OldValues = oldValues,
                    NewValues = newValues,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }

            return ValueTask.FromResult(result);
        }

        private Guid GetCurrentUserId()
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        private static Guid GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var idProp = entry.Properties.FirstOrDefault(p =>
                p.Metadata.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
            if (idProp?.CurrentValue is Guid guid) return guid;
            return Guid.Empty;
        }

        /// <summary>
        /// Returns "[REDACTED]" for properties marked with <see cref="EncryptedAttribute"/>
        /// so sensitive data never appears in audit logs.
        /// </summary>
        private static string? RedactIfEncrypted(
            Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry prop,
            bool useCurrent = false)
        {
            var clrProp = prop.Metadata.PropertyInfo;
            if (clrProp is not null &&
                clrProp.GetCustomAttributes(typeof(EncryptedAttribute), true).Length > 0)
            {
                return "[REDACTED]";
            }

            var value = useCurrent ? prop.CurrentValue : prop.OriginalValue;
            return value?.ToString();
        }
    }
}

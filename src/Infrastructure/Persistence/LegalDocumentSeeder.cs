using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Infrastructure.Persistence;

[ExcludeFromCodeCoverage]
public static class LegalDocumentSeeder
{
    public static async Task SeedLegalDocumentsAsync(this IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.LegalDocuments.AnyAsync())
        {
            logger.LogInformation("Legal documents already seeded — skipping.");
            return;
        }

        var termsContent = LoadLegalContent("TermsOfService.md", logger);
        var privacyContent = LoadLegalContent("PrivacyPolicy.md", logger);

        var effectiveDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        db.LegalDocuments.AddRange(
            new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.TermsOfService,
                Version = "1.0",
                EffectiveDate = effectiveDate,
                Content = termsContent,
                IsActive = true
            },
            new LegalDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = LegalDocumentType.PrivacyPolicy,
                Version = "1.0",
                EffectiveDate = effectiveDate,
                Content = privacyContent,
                IsActive = true
            });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded Terms of Service v1.0 and Privacy Policy v1.0.");
    }

    /// <summary>
    /// Loads a legal document by trying three sources in order:
    /// 1. Embedded resource in the entry assembly (API project)
    /// 2. File on disk relative to the entry assembly (LegalDocuments/ folder)
    /// 3. Hardcoded fallback
    /// </summary>
    private static string LoadLegalContent(string fileName, ILogger logger)
    {
        // 1. Embedded resource from the entry (API) assembly
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            var resourceName = entryAssembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (resourceName is not null)
            {
                using var stream = entryAssembly.GetManifestResourceStream(resourceName);
                if (stream is not null)
                {
                    using var reader = new StreamReader(stream);
                    logger.LogInformation("Loaded {FileName} from embedded resource.", fileName);
                    return reader.ReadToEnd();
                }
            }
        }

        // 2. File on disk next to the running assembly
        var basePath = AppContext.BaseDirectory;
        var filePath = Path.Combine(basePath, "LegalDocuments", fileName);
        if (File.Exists(filePath))
        {
            logger.LogInformation("Loaded {FileName} from disk at {Path}.", fileName, filePath);
            return File.ReadAllText(filePath);
        }

        logger.LogWarning("Could not load {FileName} — using fallback content.", fileName);
        return fileName.Contains("Terms")
            ? GetFallbackTerms()
            : GetFallbackPrivacy();
    }

    private static string GetFallbackTerms() =>
        "# ProjectPulse — Terms of Service\n\n**Version 1.0 · Effective March 1, 2026**\n\nPlease visit our website for the full Terms of Service.";

    private static string GetFallbackPrivacy() =>
        "# ProjectPulse — Privacy Policy\n\n**Version 1.0 · Effective March 1, 2026**\n\nPlease visit our website for the full Privacy Policy.";
}

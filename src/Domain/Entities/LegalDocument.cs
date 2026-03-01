using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Stores versioned legal documents (Terms of Service, Privacy Policy).
/// Only one document per type should be active at any time.
/// </summary>
public class LegalDocument
{
    public Guid Id { get; set; }
    public LegalDocumentType DocumentType { get; set; }

    /// <summary>
    /// Semantic version string (e.g., "1.0", "1.1").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Date from which this version is enforceable.
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Full document content in Markdown format.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Only one version per document type should be active.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

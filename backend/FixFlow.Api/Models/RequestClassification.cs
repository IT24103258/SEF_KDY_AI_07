namespace FixFlow.Api.Models;

/// <summary>
/// Stores the result of an AI classification (or a manager override of one) for a
/// MaintenanceRequest.  One request may have multiple rows — one per classification
/// attempt and one per manager override — so we can show the full history.
/// Inherits Id, CreatedAt, UpdatedAt, IsDeleted from BaseEntity.
/// </summary>
public class RequestClassification : BaseEntity
{
    // ── Foreign key to the request being classified ──────────────────────────
    public Guid MaintenanceRequestId { get; set; }
    public MaintenanceRequest? MaintenanceRequest { get; set; }

    // ── Classification output ─────────────────────────────────────────────────
    /// <summary>Top-level category returned by the classifier (e.g. "Plumbing").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Optional finer-grained sub-category (e.g. "Pipe Leak").</summary>
    public string? Subcategory { get; set; }

    /// <summary>
    /// Confidence score from 0.000 to 1.000.
    /// Stored as decimal(4,3) — four digits total, three after the decimal point.
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// True when the classifier is not confident enough and a human must review.
    /// Also set to true automatically when no category matches or the agent errors.
    /// </summary>
    public bool RequiresReview { get; set; }

    /// <summary>Skill the classifier thinks is needed (e.g. "Residential Plumbing &amp; Drainage Repair").</summary>
    public string? RequiredSkill { get; set; }

    /// <summary>Human-readable explanation of why this category was chosen.</summary>
    public string? Reason { get; set; }

    // ── Override tracking ─────────────────────────────────────────────────────
    /// <summary>
    /// True when this row was created by a manager manually overriding a previous
    /// AI classification rather than by the AI agent itself.
    /// </summary>
    public bool IsOverride { get; set; }

    /// <summary>
    /// Id of the manager who performed the override.  Null when IsOverride is false.
    /// </summary>
    public Guid? OverriddenByUserId { get; set; }

    /// <summary>Navigation to the manager who performed the override (if any).</summary>
    public User? OverriddenByUser { get; set; }
}

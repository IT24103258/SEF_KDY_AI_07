namespace FixFlow.Api.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// QUERY / PAGING HELPERS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Query parameters accepted by the list and "my requests" endpoints.
/// All fields are optional — omitting them returns everything, sorted newest-first.
/// </summary>
public class RequestListQueryDto
{
    /// <summary>Free-text search against Title and Description.</summary>
    public string? Search { get; set; }

    /// <summary>Filter by RequestStatus enum name (e.g. "Submitted", "Classified").</summary>
    public string? Status { get; set; }

    /// <summary>Filter by IssueCategory name (e.g. "Plumbing").</summary>
    public string? Category { get; set; }

    /// <summary>Field to sort by: CreatedAt (default), Title, Status.</summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>True = newest first (default).</summary>
    public bool SortDesc { get; set; } = true;

    /// <summary>1-based page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page, capped at 100 in the service.</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Generic paged response wrapper used by all list endpoints.
/// TotalPages is computed so the frontend doesn't have to.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// WRITE DTOs  (data sent by the client to the server)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Body for POST /api/requests — submitted by a Requester.</summary>
public class CreateRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public Guid? AssetId { get; set; }
    // CategoryId intentionally removed — category is always AI-determined via
    // ClassifyAsync, never set directly by the requester. See ADR entry on this.
}

/// <summary>
/// Body for PUT /api/requests/{id}.
/// All fields are optional — only the non-null ones are applied.
/// Only valid while Status == Submitted.
/// </summary>
public class UpdateRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? AssetId { get; set; }
    // CategoryId is intentionally not included — it's AI-owned (see CreateAsync).
}

/// <summary>
/// Body for PUT /api/requests/{id}/classification — manager override.
/// Creates a new RequestClassification row with IsOverride=true.
/// </summary>
public class ClassificationOverrideDto
{
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }
    public string? RequiredSkill { get; set; }
    public string? Reason { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// READ DTOs  (data sent by the server to the client)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Lightweight summary used in list views — avoids fetching descriptions
/// and classification history for every row.
/// </summary>
public class RequestSummaryDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Category { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>True when at least one classification exists for this request.</summary>
    public bool HasClassification { get; set; }
}

/// <summary>
/// Full request detail including description and the complete classification history.
/// Returned by GET /api/requests/{id}.
/// </summary>
public class RequestDetailDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? AssetId { get; set; }
    public string? AssetName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    /// <summary>All classification attempts/overrides, newest first.</summary>
    public List<ClassificationResultDto> Classifications { get; set; } = new();
}

/// <summary>
/// One classification record (AI result or manager override).
/// Returned inside RequestDetailDto and by GET /api/requests/{id}/classifications.
/// </summary>
public class ClassificationResultDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }
    public decimal ConfidenceScore { get; set; }
    public bool RequiresReview { get; set; }
    public string? RequiredSkill { get; set; }
    public string? Reason { get; set; }
    public bool IsOverride { get; set; }
    /// <summary>Full name of the manager who overrode (null for AI-generated rows).</summary>
    public string? OverriddenByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// IssueCategoryDto is defined in InfrastructureDtos.cs (shared DTO, same namespace)
// — no redeclaration needed here.

// ─────────────────────────────────────────────────────────────────────────────
// AGENT BRIDGE DTO  (internal — matches the Python service's JSON response shape)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The shape of the JSON the Python FastAPI classification endpoint returns.
/// Field names are snake_case on the wire; deserialized with PropertyNameCaseInsensitive=true.
/// </summary>
public class AgentClassificationResponseDto
{
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("confidence_score")]
    public decimal ConfidenceScore { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("requires_review")]
    public bool RequiresReview { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("required_skill")]
    public string? RequiredSkill { get; set; }

    public string? Reason { get; set; }
}
using FixFlow.Api.DTOs;
using Microsoft.AspNetCore.Http;
namespace FixFlow.Api.Interfaces;

/// <summary>
/// Business logic contract for Component 1 — Request Intake &amp; Classification.
/// All methods that touch data validate ownership before returning/mutating.
/// </summary>
public interface IRequestService
{
    /// <summary>Creates a new maintenance request owned by <paramref name="requesterId"/>.</summary>
    Task<RequestDetailDto> CreateAsync(CreateRequestDto dto, Guid requesterId);

    /// <summary>
    /// Returns a single request by id.
    /// A Requester can only see their own request; Manager/Admin/Technician can see any.
    /// Throws <see cref="Exceptions.NotFoundException"/> if not found or soft-deleted.
    /// Throws <see cref="Exceptions.ForbiddenException"/> if ownership check fails.
    /// </summary>
    Task<RequestDetailDto> GetByIdAsync(Guid id, Guid callerId, string callerRole);

    /// <summary>Paged list for Manager/Administrator — all non-deleted requests with optional search/filter/sort.</summary>
    Task<PagedResult<RequestSummaryDto>> GetListAsync(RequestListQueryDto query);

    /// <summary>Paged list of the caller's own requests (Requester role).</summary>
    Task<PagedResult<RequestSummaryDto>> GetMyRequestsAsync(Guid requesterId, RequestListQueryDto query);

    /// <summary>
    /// Applies a partial update.  Only fields supplied (non-null) are changed.
    /// Blocked unless <c>Status == Submitted</c>.
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the caller doesn't own the request and is not staff.
    /// Throws <see cref="Exceptions.ValidationException"/> if the status guard fails.
    /// </summary>
    Task<RequestDetailDto> UpdateAsync(Guid id, UpdateRequestDto dto, Guid callerId, string callerRole);

    /// <summary>
    /// Soft-deletes a request (sets IsDeleted=true).
    /// Blocked unless <c>Status == Submitted</c>.
    /// </summary>
    Task SoftDeleteAsync(Guid id, Guid callerId, string callerRole);

    /// <summary>
    /// Calls the Python classification agent, validates its output against seeded categories/skills,
    /// stores the result as a <c>RequestClassification</c> row, and advances the request status.
    /// Retries once on failure; falls back to category="Uncategorized"/requires_review=true rather than crashing.
    /// </summary>
    Task<ClassificationResultDto> ClassifyAsync(Guid id, Guid callerId, string callerRole);

    /// <summary>Returns all classification rows for a request, newest first.</summary>
    Task<List<ClassificationResultDto>> GetClassificationHistoryAsync(Guid id);

    /// <summary>
    /// Manager creates a manual override classification row (IsOverride=true, OverriddenByUserId set).
    /// The request status is advanced to <c>Classified</c>.
    /// </summary>
    Task<ClassificationResultDto> OverrideClassificationAsync(Guid id, ClassificationOverrideDto dto, Guid managerId);

    /// <summary>Returns all active issue categories — used to populate the category dropdown.</summary>
    Task<List<IssueCategoryDto>> GetCategoriesAsync();

    /// <summary>
    /// Uploads a file to Cloudinary, creates a RequestAttachment row, and returns the attachment DTO.
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the caller doesn't own the request and is not staff.
    /// </summary>
    Task<AttachmentDto> UploadAttachmentAsync(Guid requestId, IFormFile file, Guid callerId);
}

/// <summary>
/// HTTP client contract for the Python FastAPI classification microservice.
/// Returns null on timeout, HTTP error, or any network failure — callers must handle the null case.
/// </summary>
/// <summary>
/// HTTP client contract for the Python FastAPI classification microservice.
/// Returns null on timeout, HTTP error, or any network failure — callers must handle the null case.
/// </summary>
public interface IClassificationAgentService
{
    Task<AgentClassificationResponseDto?> ClassifyAsync(string title, string description, CancellationToken ct = default);
}

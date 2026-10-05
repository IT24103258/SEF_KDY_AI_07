using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

/// <summary>
/// Component 1 — Request Intake &amp; Classification.
/// Route prefix: /api/requests  (NOT /api/maintenance-requests).
///
/// All user identity comes from JWT claims — never from the request body.
/// Ownership checks and status guards are enforced in RequestService.
/// </summary>
[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService)
    {
        _requestService = requestService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — extract verified claims from the JWT
    // ─────────────────────────────────────────────────────────────────────────

    private Guid GetCallerId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("Could not read caller identity from token.");
    }

    private string GetCallerRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/requests  — Requester submits a new request
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Requester")]
    public async Task<ActionResult<ApiResponse<RequestDetailDto>>> CreateRequest(
        [FromBody] CreateRequestDto dto)
    {
        var requesterId = GetCallerId();
        var result = await _requestService.CreateAsync(dto, requesterId);
        return CreatedAtAction(
            nameof(GetRequestById),
            new { id = result.Id },
            ApiResponse<RequestDetailDto>.SuccessResult(result, "Request submitted successfully."));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/requests  — Manager / Admin — paged queue of ALL requests
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<ActionResult<ApiResponse<PagedResult<RequestSummaryDto>>>> GetRequests(
        [FromQuery] RequestListQueryDto query)
    {
        var result = await _requestService.GetListAsync(query);
        return Ok(ApiResponse<PagedResult<RequestSummaryDto>>.SuccessResult(result));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/requests/me  — Requester — their own requests only
    // NOTE: this route MUST be declared before {id} so "me" isn't parsed as a Guid
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("me")]
    [Authorize(Roles = "Requester")]
    public async Task<ActionResult<ApiResponse<PagedResult<RequestSummaryDto>>>> GetMyRequests(
        [FromQuery] RequestListQueryDto query)
    {
        var requesterId = GetCallerId();
        var result = await _requestService.GetMyRequestsAsync(requesterId, query);
        return Ok(ApiResponse<PagedResult<RequestSummaryDto>>.SuccessResult(result));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/requests/{id}  — owner or staff
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RequestDetailDto>>> GetRequestById(Guid id)
    {
        var result = await _requestService.GetByIdAsync(id, GetCallerId(), GetCallerRole());
        return Ok(ApiResponse<RequestDetailDto>.SuccessResult(result));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT /api/requests/{id}  — owner or staff; blocked unless Status == Submitted
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RequestDetailDto>>> UpdateRequest(
        Guid id, [FromBody] UpdateRequestDto dto)
    {
        var result = await _requestService.UpdateAsync(id, dto, GetCallerId(), GetCallerRole());
        return Ok(ApiResponse<RequestDetailDto>.SuccessResult(result, "Request updated successfully."));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE /api/requests/{id}  — owner or staff; blocked unless Status == Submitted
    // ─────────────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRequest(Guid id)
    {
        await _requestService.SoftDeleteAsync(id, GetCallerId(), GetCallerRole());
        return Ok(ApiResponse<object>.SuccessResult(new { }, "Request deleted successfully."));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/requests/{id}/classify  — Manager or Administrator triggers AI
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/classify")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<ActionResult<ApiResponse<ClassificationResultDto>>> ClassifyRequest(Guid id)
    {
        var result = await _requestService.ClassifyAsync(id, GetCallerId(), GetCallerRole());
        return Ok(ApiResponse<ClassificationResultDto>.SuccessResult(
            result, "Request classification completed."));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/requests/{id}/classifications  — any authenticated user
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/classifications")]
    public async Task<ActionResult<ApiResponse<List<ClassificationResultDto>>>> GetClassifications(
        Guid id)
    {
        var result = await _requestService.GetClassificationHistoryAsync(id);
        return Ok(ApiResponse<List<ClassificationResultDto>>.SuccessResult(result));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT /api/requests/{id}/classification  — Manager override (audited)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}/classification")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<ActionResult<ApiResponse<ClassificationResultDto>>> OverrideClassification(
        Guid id, [FromBody] ClassificationOverrideDto dto)
    {
        var managerId = GetCallerId();
        var result = await _requestService.OverrideClassificationAsync(id, dto, managerId);
        return Ok(ApiResponse<ClassificationResultDto>.SuccessResult(
            result, "Classification override recorded successfully."));
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> UploadAttachment(
        Guid id, IFormFile file)
    {
        var result = await _requestService.UploadAttachmentAsync(id, file, GetCallerId());
        return Ok(ApiResponse<AttachmentDto>.SuccessResult(result, "Photo uploaded successfully."));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Separate controller for /api/issue-categories
// (Different route prefix — kept in this file since it is owned by Component 1)
// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/issue-categories")]
[Authorize]
public class IssueCategoriesController : ControllerBase
{
    private readonly IRequestService _requestService;

    public IssueCategoriesController(IRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// Returns all active issue categories.
    /// Used by the React and Flutter forms to populate the category dropdown.
    /// Any authenticated role may call this endpoint.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<IssueCategoryDto>>>> GetCategories()
    {
        var categories = await _requestService.GetCategoriesAsync();
        return Ok(ApiResponse<List<IssueCategoryDto>>.SuccessResult(categories));
    }
}

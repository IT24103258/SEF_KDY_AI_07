using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

/// <summary>
/// Business logic for Component 1 — Request Intake &amp; Classification.
/// All public methods validate ownership before touching data, and every
/// classification attempt goes through deterministic validation before
/// being persisted, falling back safely rather than crashing.
/// </summary>
public class RequestService : IRequestService
{
    private readonly FixFlowDbContext _context;
    private readonly IClassificationAgentService _classificationAgent;
    private readonly IPriorityAgentService _priorityAgent;
    private readonly ILogger<RequestService> _logger;

    // Roles that bypass the "own requests only" ownership gate
    private static readonly HashSet<string> _staffRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Manager", "Administrator", "Technician" };

    public RequestService(
    FixFlowDbContext context,
    IClassificationAgentService classificationAgent,
    IPriorityAgentService priorityAgent,
    ILogger<RequestService> logger)
{
    _context = context;
    _classificationAgent = classificationAgent;
    _priorityAgent = priorityAgent;
    _logger = logger;
}

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<RequestDetailDto> CreateAsync(CreateRequestDto dto, Guid requesterId)
    {
        // Generate sequential number within the current calendar year: REQ-YYYY-NNNN
        var year = DateTime.UtcNow.Year;
        var prefix = $"REQ-{year}-";
        var existingCount = await _context.MaintenanceRequests
            .CountAsync(r => r.RequestNumber.StartsWith(prefix));
        var requestNumber = $"{prefix}{(existingCount + 1):D4}";

               var request = new MaintenanceRequest
        {
            RequestNumber = requestNumber,
            Title         = dto.Title.Trim(),
            Description   = dto.Description.Trim(),
            Status        = RequestStatus.Submitted,
            LocationId    = dto.LocationId,
            AssetId       = dto.AssetId,
            RequesterId   = requesterId
            // CategoryId stays null until ClassifyAsync sets it
        };

        _context.MaintenanceRequests.Add(request);
        await _context.SaveChangesAsync();

        return await FetchDetailAsync(request.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET BY ID  (with ownership gate)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<RequestDetailDto> GetByIdAsync(Guid id, Guid callerId, string callerRole)
    {
        var request = await _context.MaintenanceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        if (!IsStaff(callerRole) && request.RequesterId != callerId)
            throw new ForbiddenException("You do not have permission to view this request.");

        return await FetchDetailAsync(id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PAGED LISTS
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<PagedResult<RequestSummaryDto>> GetListAsync(RequestListQueryDto query)
        => await BuildPagedResultAsync(
            _context.MaintenanceRequests.Where(r => !r.IsDeleted),
            query);

    public async Task<PagedResult<RequestSummaryDto>> GetMyRequestsAsync(
        Guid requesterId, RequestListQueryDto query)
        => await BuildPagedResultAsync(
            _context.MaintenanceRequests.Where(r => !r.IsDeleted && r.RequesterId == requesterId),
            query);

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE  (status-guarded)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<RequestDetailDto> UpdateAsync(
        Guid id, UpdateRequestDto dto, Guid callerId, string callerRole)
    {
        var request = await _context.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        if (request.RequesterId != callerId)
            throw new ForbiddenException("Only the request's owner may edit it.");

        if (request.Status != RequestStatus.Submitted)
            throw new ValidationException(
                $"Requests can only be edited while in 'Submitted' status. Current status: {request.Status}.");

        // Apply only the non-null fields sent by the client (partial update)
                if (dto.Title        != null)          request.Title       = dto.Title.Trim();
        if (dto.Description  != null)          request.Description = dto.Description.Trim();
        if (dto.LocationId.HasValue)           request.LocationId  = dto.LocationId.Value;
        if (dto.AssetId      != null)          request.AssetId     = dto.AssetId;       // allows explicit clear via null
        // CategoryId is intentionally not editable here — it's AI-owned (see CreateAsync).
        
        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await FetchDetailAsync(id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SOFT DELETE  (status-guarded)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task SoftDeleteAsync(Guid id, Guid callerId, string callerRole)
    {
        var request = await _context.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        if (request.RequesterId != callerId)
            throw new ForbiddenException("Only the request's owner may edit it.");

        if (request.Status != RequestStatus.Submitted)
            throw new ValidationException(
                $"Requests can only be deleted while in 'Submitted' status. Current status: {request.Status}.");

        request.IsDeleted = true;
        request.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CLASSIFY  (calls Python agent with retry + fallback)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ClassificationResultDto> ClassifyAsync(
        Guid id, Guid callerId, string callerRole)
    {
        /*var request = await _context.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");*/
        var request = await _context.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        if (request.CategoryId != null)
            throw new ValidationException(
                "This request already has a manually-selected category. AI classification is only for requests where the requester left the category blank.");

        // Load the reference data we need for deterministic validation
        var validCategories = await _context.IssueCategories
            .Where(c => !c.IsDeleted)
            .Select(c => c.Name)
            .ToListAsync();

        var validSkills = await _context.Skills
            .Where(s => !s.IsDeleted)
            .Select(s => s.Name)
            .ToListAsync();

        // Call agent — retry once on any failure, then fall back gracefully
        AgentClassificationResponseDto? agentResult = null;
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            agentResult = await _classificationAgent.ClassifyAsync(request.Title, request.Description);
            if (agentResult != null)
            {
                _logger.LogInformation(
                    "Classification agent succeeded on attempt {Attempt} for request {Id}.", attempt, id);
                break;
            }
            _logger.LogWarning(
                "Classification agent attempt {Attempt} returned null for request {Id}.", attempt, id);
        }

        // Validate agent output; replace with safe defaults if anything is wrong
        var classification = BuildClassificationRow(agentResult, validCategories, validSkills, id);

        _context.Set<RequestClassification>().Add(classification);

        if (classification.Category != "Uncategorized")
        {
            var categoryId = await _context.IssueCategories
                .Where(c => c.Name == classification.Category && !c.IsDeleted)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();
            
            if (categoryId != Guid.Empty)
            {
                request.CategoryId = categoryId;
            }
        }

        // Advance status based on whether the result needs human review
        request.Status    = classification.RequiresReview ? RequestStatus.InReview : RequestStatus.Classified;
request.UpdatedAt = DateTime.UtcNow;

await _context.SaveChangesAsync();

// Start Component 2 only when Component 1 produced a usable classification.
// Requests requiring human review or falling back to "Uncategorized"
// must not be automatically risk-assessed.
if (!classification.RequiresReview &&
    !string.Equals(classification.Category, "Uncategorized", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        await _priorityAgent.EvaluateAndPersistAsync(
            id,
            null,
            "PriorityAgent");
    }
    catch (Exception ex)
    {
        // Classification has already been safely persisted.
        // Log the Component 2 failure without losing the request/classification.
        _logger.LogError(
            ex,
            "PriorityAgent workflow failed for classified request {RequestId}.",
            id);
    }
}

return MapClassificationToDto(classification, null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CLASSIFICATION HISTORY
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<ClassificationResultDto>> GetClassificationHistoryAsync(Guid id)
    {
        var exists = await _context.MaintenanceRequests
            .AnyAsync(r => r.Id == id && !r.IsDeleted);

        if (!exists)
            throw new NotFoundException($"Request '{id}' was not found.");

        var rows = await _context.Set<RequestClassification>()
            .Where(rc => rc.MaintenanceRequestId == id && !rc.IsDeleted)
            .Include(rc => rc.OverriddenByUser)
            .OrderByDescending(rc => rc.CreatedAt)
            .ToListAsync();

        return rows.Select(rc => MapClassificationToDto(rc, rc.OverriddenByUser)).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MANAGER OVERRIDE
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ClassificationResultDto> OverrideClassificationAsync(
        Guid id, ClassificationOverrideDto dto, Guid managerId)
    {
        var request = await _context.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        var overrideRow = new RequestClassification
        {
            MaintenanceRequestId = id,
            Category             = dto.Category.Trim(),
            Subcategory          = dto.Subcategory?.Trim(),
            ConfidenceScore      = 1.0m,   // a manager override is treated as fully confident
            RequiresReview       = false,
            RequiredSkill        = dto.RequiredSkill?.Trim(),
            Reason               = dto.Reason?.Trim(),
            IsOverride           = true,
            OverriddenByUserId   = managerId
        };

        _context.Set<RequestClassification>().Add(overrideRow);

        var categoryId = await _context.IssueCategories
            .Where(c => c.Name == overrideRow.Category && !c.IsDeleted)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
            
        if (categoryId != Guid.Empty)
        {
            request.CategoryId = categoryId;
        }

        // A manager override always marks the request as fully classified
        request.Status    = RequestStatus.Classified;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var manager = await _context.Users.FindAsync(managerId);
        return MapClassificationToDto(overrideRow, manager);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ISSUE CATEGORIES  (for the dropdown)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<IssueCategoryDto>> GetCategoriesAsync()
    {
        return await _context.IssueCategories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new IssueCategoryDto
            {
                Id              = c.Id,
                Name            = c.Name,
                Description     = c.Description,
                DefaultPriority = c.DefaultPriority
            })
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static bool IsStaff(string role) => _staffRoles.Contains(role);

    /// <summary>
    /// Applies optional search / filter / sort to a base query, executes it with
    /// paging, and returns a typed PagedResult.
    /// All filtering happens in SQL; enum-to-string mapping happens in memory after
    /// ToListAsync() — consistent with the pattern in AgentWorkflowService.
    /// </summary>
    private async Task<PagedResult<RequestSummaryDto>> BuildPagedResultAsync(
        IQueryable<MaintenanceRequest> baseQuery,
        RequestListQueryDto query)
    {
        // ── Filters ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.Title.ToLower().Contains(s) ||
                r.Description.ToLower().Contains(s) ||
                r.RequestNumber.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<RequestStatus>(query.Status, ignoreCase: true, out var statusEnum))
        {
            baseQuery = baseQuery.Where(r => r.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var cat = query.Category.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.Category != null && r.Category.Name.ToLower() == cat);
        }

        // ── Sort ──────────────────────────────────────────────────────────────
        baseQuery = (query.SortBy?.ToLower(), query.SortDesc) switch
        {
            ("title",  true)  => baseQuery.OrderByDescending(r => r.Title),
            ("title",  false) => baseQuery.OrderBy(r => r.Title),
            ("status", true)  => baseQuery.OrderByDescending(r => r.Status),
            ("status", false) => baseQuery.OrderBy(r => r.Status),
            (_,        true)  => baseQuery.OrderByDescending(r => r.CreatedAt),
            (_,        false) => baseQuery.OrderBy(r => r.CreatedAt),
        };

        // ── Paging ────────────────────────────────────────────────────────────
        var pageSize    = Math.Clamp(query.PageSize, 1, 100);
        var page        = Math.Max(query.Page, 1);
        var totalCount  = await baseQuery.CountAsync();

        // Load entities with navigations — enum ToString() runs in C# after fetch
        var entities = await baseQuery
            .Include(r => r.Requester)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        // Single extra query to find which requests already have a classification
        var pageIds = entities.Select(r => r.Id).ToList();
        var classifiedIds = (await _context.Set<RequestClassification>()
            .Where(rc => pageIds.Contains(rc.MaintenanceRequestId) && !rc.IsDeleted)
            .Select(rc => rc.MaintenanceRequestId)
            .Distinct()
            .ToListAsync()).ToHashSet();

        var items = entities.Select(r => new RequestSummaryDto
        {
            Id                = r.Id,
            RequestNumber     = r.RequestNumber,
            Title             = r.Title,
            Status            = r.Status.ToString(),
            Category          = r.Category?.Name,
            RequesterId       = r.RequesterId,
            RequesterName     = r.Requester != null
                ? $"{r.Requester.FirstName} {r.Requester.LastName}"
                : string.Empty,
            LocationName      = r.Location?.Name,
            CreatedAt         = r.CreatedAt,
            HasClassification = classifiedIds.Contains(r.Id)
        }).ToList();

        return new PagedResult<RequestSummaryDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    /// <summary>
    /// Reloads a single request with all navigations and its classification history.
    /// Called after create/update so we always return a consistent, fully-populated DTO.
    /// </summary>
    private async Task<RequestDetailDto> FetchDetailAsync(Guid id)
    {
        var r = await _context.MaintenanceRequests
            .Include(r => r.Requester)
            .Include(r => r.Location)
            .Include(r => r.Asset)
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException($"Request '{id}' was not found.");

        var classifications = await _context.Set<RequestClassification>()
            .Where(rc => rc.MaintenanceRequestId == id && !rc.IsDeleted)
            .Include(rc => rc.OverriddenByUser)
            .OrderByDescending(rc => rc.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return new RequestDetailDto
        {
            Id              = r.Id,
            RequestNumber   = r.RequestNumber,
            Title           = r.Title,
            Description     = r.Description,
            Status          = r.Status.ToString(),
            RequesterId     = r.RequesterId,
            RequesterName   = r.Requester != null
                ? $"{r.Requester.FirstName} {r.Requester.LastName}"
                : string.Empty,
            LocationId      = r.LocationId,
            LocationName    = r.Location?.Name,
            AssetId         = r.AssetId,
            AssetName       = r.Asset?.Name,
            CategoryId      = r.CategoryId,
            CategoryName    = r.Category?.Name,
            CreatedAt       = r.CreatedAt,
            UpdatedAt       = r.UpdatedAt,
            Classifications = classifications
                .Select(rc => MapClassificationToDto(rc, rc.OverriddenByUser))
                .ToList()
        };
    }

    /// <summary>
    /// Validates the agent's response against seeded categories and skills.
    /// Returns a safe fallback classification row if anything fails validation.
    /// This is the single choke-point for all output sanitisation — the caller
    /// never needs to check whether the agent's data is trustworthy.
    /// </summary>
    private static RequestClassification BuildClassificationRow(
        AgentClassificationResponseDto? agent,
        List<string> validCategories,
        List<string> validSkills,
        Guid requestId)
    {
        const string FallbackCategory = "Uncategorized";
        const decimal FallbackScore   = 0.0m;

        if (agent == null)
        {
            return new RequestClassification
            {
                MaintenanceRequestId = requestId,
                Category             = FallbackCategory,
                ConfidenceScore      = FallbackScore,
                RequiresReview       = true,
                Reason               = "Classification agent did not respond after 2 attempts. Manual review required.",
                IsOverride           = false
            };
        }

        // Confidence must be in [0, 1]
        bool scoreInvalid = agent.ConfidenceScore < 0m || agent.ConfidenceScore > 1m;

        // Category must match one of the seeded IssueCategory names (case-insensitive)
        var matchedCategory = validCategories
            .FirstOrDefault(c => c.Equals(agent.Category?.Trim(), StringComparison.OrdinalIgnoreCase));
        bool categoryInvalid = matchedCategory == null;

        // If a skill is provided it must match a seeded Skill name (case-insensitive)
        var skill = agent.RequiredSkill?.Trim();
        bool skillInvalid = !string.IsNullOrEmpty(skill) &&
            !validSkills.Any(s => s.Equals(skill, StringComparison.OrdinalIgnoreCase));

        if (categoryInvalid || scoreInvalid || skillInvalid)
        {
            return new RequestClassification
            {
                MaintenanceRequestId = requestId,
                Category             = FallbackCategory,
                ConfidenceScore      = FallbackScore,
                RequiresReview       = true,
                Reason               = $"Agent output failed validation " +
                                       $"(category valid: {!categoryInvalid}, " +
                                       $"score valid: {!scoreInvalid}, " +
                                       $"skill valid: {!skillInvalid}). Manual review required.",
                IsOverride           = false
            };
        }

        // All checks passed — persist the agent's output
        return new RequestClassification
        {
            MaintenanceRequestId = requestId,
            Category             = matchedCategory!,
            Subcategory          = agent.Subcategory?.Trim(),
            ConfidenceScore      = agent.ConfidenceScore,
            RequiresReview       = agent.RequiresReview,
            RequiredSkill        = skill,
            Reason               = agent.Reason?.Trim(),
            IsOverride           = false
        };
    }

    private static ClassificationResultDto MapClassificationToDto(
        RequestClassification rc, User? overrideUser) =>
        new()
        {
            Id                   = rc.Id,
            Category             = rc.Category,
            Subcategory          = rc.Subcategory,
            ConfidenceScore      = rc.ConfidenceScore,
            RequiresReview       = rc.RequiresReview,
            RequiredSkill        = rc.RequiredSkill,
            Reason               = rc.Reason,
            IsOverride           = rc.IsOverride,
            OverriddenByUserName = overrideUser != null
                ? $"{overrideUser.FirstName} {overrideUser.LastName}"
                : null,
            CreatedAt            = rc.CreatedAt
        };
}

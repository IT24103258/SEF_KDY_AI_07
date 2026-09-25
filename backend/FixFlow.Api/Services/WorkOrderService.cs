using System.Text.Json;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly FixFlowDbContext _context;
    private readonly ISchedulingService _schedulingService;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;

    public WorkOrderService(
        FixFlowDbContext context,
        ISchedulingService schedulingService,
        IAuditLogService auditLogService,
        INotificationService notificationService)
    {
        _context = context;
        _schedulingService = schedulingService;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
    }

    public async Task<PagedResultDto<WorkOrderSummaryDto>> GetWorkOrdersAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? status = null,
        Guid? technicianId = null,
        string? priority = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? sortBy = null,
        string? sortDirection = null,
        Guid? currentUserId = null,
        string? currentUserRole = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .Include(w => w.Location)
            .Where(w => !w.IsDeleted);

        // Role-based filtering
        if (currentUserRole == "Technician" && currentUserId.HasValue)
        {
            query = query.Where(w => w.Technician != null && w.Technician.UserId == currentUserId.Value);
        }
        else if (currentUserRole == "Requester" && currentUserId.HasValue)
        {
            query = query.Where(w => w.Request != null && w.Request.RequesterId == currentUserId.Value);
        }

        // Search text
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(w =>
                w.WorkOrderNumber.ToLower().Contains(searchLower) ||
                w.Title.ToLower().Contains(searchLower) ||
                w.Description.ToLower().Contains(searchLower) ||
                (w.Request != null && w.Request.RequestNumber.ToLower().Contains(searchLower)) ||
                (w.Technician != null && w.Technician.User != null &&
                    (w.Technician.User.FirstName.ToLower().Contains(searchLower) ||
                     w.Technician.User.LastName.ToLower().Contains(searchLower))) ||
                (w.Location != null && w.Location.Name.ToLower().Contains(searchLower)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkOrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(w => w.Status == parsedStatus);
        }

        // Technician filter
        if (technicianId.HasValue && technicianId.Value != Guid.Empty)
        {
            query = query.Where(w => w.TechnicianId == technicianId.Value);
        }

        // Priority filter
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<WorkOrderPriority>(priority, true, out var parsedPriority))
        {
            query = query.Where(w => w.Priority == parsedPriority);
        }

        // Date range
        if (startDate.HasValue)
            query = query.Where(w => w.ScheduledStartTime >= startDate.Value || w.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.ScheduledStartTime <= endDate.Value || w.CreatedAt <= endDate.Value);

        // Sorting
        var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLower()) switch
        {
            "priority" => isAscending ? query.OrderBy(w => w.Priority) : query.OrderByDescending(w => w.Priority),
            "status" => isAscending ? query.OrderBy(w => w.Status) : query.OrderByDescending(w => w.Status),
            "start" or "scheduled" or "scheduledstarttime" => isAscending ? query.OrderBy(w => w.ScheduledStartTime) : query.OrderByDescending(w => w.ScheduledStartTime),
            "technician" => isAscending
                ? query.OrderBy(w => w.Technician != null && w.Technician.User != null ? w.Technician.User.LastName : string.Empty)
                : query.OrderByDescending(w => w.Technician != null && w.Technician.User != null ? w.Technician.User.LastName : string.Empty),
            _ => isAscending ? query.OrderBy(w => w.CreatedAt) : query.OrderByDescending(w => w.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WorkOrderSummaryDto
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                Title = w.Title,
                RequestId = w.RequestId,
                RequestNumber = w.Request != null ? w.Request.RequestNumber : string.Empty,
                TechnicianName = w.Technician != null && w.Technician.User != null ? $"{w.Technician.User.FirstName} {w.Technician.User.LastName}" : "Unassigned",
                LocationName = w.Location != null ? w.Location.Name : "Unassigned",
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                ScheduledStartTime = w.ScheduledStartTime,
                ScheduledEndTime = w.ScheduledEndTime,
                EstimatedDurationMinutes = w.EstimatedDurationMinutes,
                SLADeadline = w.SLADeadline,
                ConflictDetected = w.ConflictDetected,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<WorkOrderSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<WorkOrderDto> GetWorkOrderByIdAsync(Guid id, Guid? currentUserId = null, string? currentUserRole = null)
    {
        var w = await _context.Set<WorkOrder>()
            .Include(x => x.Request)
            .Include(x => x.Technician)
                .ThenInclude(t => t!.User)
            .Include(x => x.Location)
            .Include(x => x.ApprovedBy)
            .Include(x => x.StatusHistories)
                .ThenInclude(h => h.ChangedBy)
            .Include(x => x.Notes)
                .ThenInclude(n => n.Author)
            .Include(x => x.Evidence)
                .ThenInclude(e => e.UploadedBy)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (w == null)
            throw new NotFoundException($"Work order with ID '{id}' was not found.");

        // Scoping checks
        if (currentUserRole == "Technician" && currentUserId.HasValue && w.Technician?.UserId != currentUserId.Value)
            throw new UnauthorizedAccessException("Technicians may only access work orders assigned to them.");

        if (currentUserRole == "Requester" && currentUserId.HasValue && w.Request?.RequesterId != currentUserId.Value)
            throw new UnauthorizedAccessException("Requesters may only view work orders for their own requests.");

        return MapToDto(w);
    }

    public async Task<WorkOrderDto> CreateWorkOrderAsync(WorkOrderCreateDto dto, Guid creatorUserId)
    {
        if (dto.RequestId == Guid.Empty)
            throw new ArgumentException("A valid Maintenance Request ID is required.");

        var req = await _context.MaintenanceRequests
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == dto.RequestId);
        if (req == null)
            throw new NotFoundException($"Maintenance request '{dto.RequestId}' was not found.");

        if (dto.TechnicianId == Guid.Empty)
            throw new ArgumentException("A valid Technician ID is required.");

        var tech = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == dto.TechnicianId || t.UserId == dto.TechnicianId);
        if (tech == null)
            throw new NotFoundException($"Technician '{dto.TechnicianId}' was not found.");

        var workOrderCount = await _context.Set<WorkOrder>().CountAsync() + 1;
        var workOrderNumber = $"WO-{DateTime.UtcNow:yyyyMM}-{workOrderCount:D4}";

        var priority = Enum.TryParse<WorkOrderPriority>(dto.Priority, true, out var p) ? p : WorkOrderPriority.Medium;

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = workOrderNumber,
            Title = dto.Title,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? req.Description : dto.Description,
            RequestId = req.Id,
            TechnicianId = tech.Id,
            LocationId = dto.LocationId ?? req.LocationId,
            Priority = priority,
            Status = dto.ScheduledStartTime.HasValue ? WorkOrderStatus.Scheduled : WorkOrderStatus.Draft,
            ScheduledStartTime = dto.ScheduledStartTime,
            ScheduledEndTime = dto.ScheduledEndTime ?? dto.ScheduledStartTime?.AddMinutes(dto.EstimatedDurationMinutes),
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            SLADeadline = dto.SLADeadline
        };

        // If scheduled directly, validate times
        if (workOrder.ScheduledStartTime.HasValue && workOrder.ScheduledEndTime.HasValue)
        {
            var valResult = await _schedulingService.ValidateScheduleAsync(
                workOrder.TechnicianId,
                workOrder.ScheduledStartTime.Value,
                workOrder.ScheduledEndTime.Value,
                workOrder.EstimatedDurationMinutes,
                dto.Priority,
                dto.SLADeadline);

            if (!valResult.IsValid)
            {
                workOrder.ConflictDetected = true;
                workOrder.ConflictDetailsJson = JsonSerializer.Serialize(valResult.Conflicts);
            }
        }

        await _context.Set<WorkOrder>().AddAsync(workOrder);

        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = WorkOrderStatus.Draft,
            NewStatus = workOrder.Status,
            ChangedById = creatorUserId,
            Reason = "Work order created with assigned technician."
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            creatorUserId,
            "WorkOrderCreated",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrderNumber, workOrder.Status, workOrder.RequestId, workOrder.TechnicianId }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<WorkOrderDto> UpdateWorkOrderAsync(Guid id, WorkOrderUpdateDto dto, Guid updaterUserId)
    {
        var workOrder = await _context.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);
        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        if (workOrder.Status == WorkOrderStatus.Completed || workOrder.Status == WorkOrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot modify a work order that is already in '{workOrder.Status}' state.");

        workOrder.Title = dto.Title;
        workOrder.Description = dto.Description;
        workOrder.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;

        if (dto.LocationId.HasValue) workOrder.LocationId = dto.LocationId;
        if (dto.TechnicianId.HasValue) workOrder.TechnicianId = dto.TechnicianId.Value;

        if (Enum.TryParse<WorkOrderPriority>(dto.Priority, true, out var p))
            workOrder.Priority = p;

        if (dto.ScheduledStartTime.HasValue)
        {
            workOrder.ScheduledStartTime = dto.ScheduledStartTime;
            workOrder.ScheduledEndTime = dto.ScheduledEndTime ?? dto.ScheduledStartTime.Value.AddMinutes(dto.EstimatedDurationMinutes);

            // Re-validate schedule
            var valResult = await _schedulingService.ValidateScheduleAsync(
                workOrder.TechnicianId,
                workOrder.ScheduledStartTime.Value,
                workOrder.ScheduledEndTime.Value,
                workOrder.EstimatedDurationMinutes,
                dto.Priority,
                workOrder.SLADeadline,
                workOrder.Id);

            workOrder.ConflictDetected = !valResult.IsValid;
            workOrder.ConflictDetailsJson = JsonSerializer.Serialize(valResult.Conflicts);
        }

        workOrder.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            updaterUserId,
            "WorkOrderUpdated",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrder.WorkOrderNumber, dto.Title, workOrder.ScheduledStartTime }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<bool> DeleteWorkOrderAsync(Guid id, Guid deleterUserId)
    {
        var workOrder = await _context.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);
        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        if (workOrder.Status == WorkOrderStatus.InProgress || workOrder.Status == WorkOrderStatus.Completed)
        {
            // Do not hard delete active/completed work orders to preserve audit trail
            workOrder.Status = WorkOrderStatus.Cancelled;
            workOrder.UpdatedAt = DateTime.UtcNow;

            var history = new WorkOrderStatusHistory
            {
                WorkOrderId = workOrder.Id,
                PreviousStatus = workOrder.Status,
                NewStatus = WorkOrderStatus.Cancelled,
                ChangedById = deleterUserId,
                Reason = "Work order cancelled due to deletion request on active job."
            };
            await _context.Set<WorkOrderStatusHistory>().AddAsync(history);
        }
        else
        {
            workOrder.IsDeleted = true;
            workOrder.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            deleterUserId,
            "WorkOrderDeleted",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrder.WorkOrderNumber, isSoftDeleted = workOrder.IsDeleted, newStatus = workOrder.Status }),
            "127.0.0.1");

        return true;
    }

    public async Task<WorkOrderDto> UpdateStatusAsync(Guid id, WorkOrderStatusUpdateDto dto, Guid currentUserId, string currentUserRole)
    {
        var workOrder = await _context.Set<WorkOrder>()
            .Include(w => w.Technician)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        if (!Enum.TryParse<WorkOrderStatus>(dto.Status, true, out var targetStatus))
            throw new ArgumentException($"Invalid status string '{dto.Status}'.");

        var currentStatus = workOrder.Status;

        // Role-based state transition enforcement
        if (currentUserRole == "Technician")
        {
            if (workOrder.Technician?.UserId != currentUserId)
                throw new UnauthorizedAccessException("You can only change status for work orders assigned to you.");

            // Technicians can only perform operational transitions
            var allowedTechTransitions = new Dictionary<WorkOrderStatus, List<WorkOrderStatus>>
            {
                { WorkOrderStatus.Scheduled, new List<WorkOrderStatus> { WorkOrderStatus.InProgress } },
                { WorkOrderStatus.Approved, new List<WorkOrderStatus> { WorkOrderStatus.InProgress } },
                { WorkOrderStatus.InProgress, new List<WorkOrderStatus> { WorkOrderStatus.Paused, WorkOrderStatus.Completed } },
                { WorkOrderStatus.Paused, new List<WorkOrderStatus> { WorkOrderStatus.InProgress, WorkOrderStatus.Completed } }
            };

            if (!allowedTechTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(targetStatus))
            {
                throw new InvalidOperationException($"Technicians cannot transition status from '{currentStatus}' to '{targetStatus}'.");
            }
        }
        else
        {
            // Server-side State Machine Rules
            ValidateStateTransition(currentStatus, targetStatus);
        }

        workOrder.Status = targetStatus;
        workOrder.UpdatedAt = DateTime.UtcNow;

        if (targetStatus == WorkOrderStatus.InProgress && !workOrder.ActualStartTime.HasValue)
        {
            workOrder.ActualStartTime = DateTime.UtcNow;
        }
        else if (targetStatus == WorkOrderStatus.Completed && !workOrder.ActualEndTime.HasValue)
        {
            workOrder.ActualEndTime = DateTime.UtcNow;
        }

        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = currentStatus,
            NewStatus = targetStatus,
            ChangedById = currentUserId,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? $"Status changed from {currentStatus} to {targetStatus}." : dto.Reason
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            currentUserId,
            "WorkOrderStatusChanged",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { from = currentStatus.ToString(), to = targetStatus.ToString(), reason = dto.Reason }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<WorkOrderDto> ApproveWorkOrderAsync(Guid id, WorkOrderApprovalDecisionDto decision, Guid approverUserId)
    {
        var workOrder = await _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        if (workOrder.Status != WorkOrderStatus.PendingManagerApproval && workOrder.Status != WorkOrderStatus.Proposed)
        {
            throw new InvalidOperationException($"Cannot approve work order in '{workOrder.Status}' state. Must be in 'PendingManagerApproval'.");
        }

        // Re-run deterministic validation inside transaction to prevent concurrency races
        if (workOrder.ScheduledStartTime.HasValue && workOrder.ScheduledEndTime.HasValue)
        {
            var valResult = await _schedulingService.ValidateScheduleAsync(
                workOrder.TechnicianId,
                workOrder.ScheduledStartTime.Value,
                workOrder.ScheduledEndTime.Value,
                workOrder.EstimatedDurationMinutes,
                workOrder.Priority.ToString(),
                workOrder.SLADeadline,
                workOrder.Id);

            if (!valResult.IsValid && !decision.Approved)
            {
                throw new InvalidOperationException($"Schedule validation failed: {string.Join("; ", valResult.ValidationErrors)}");
            }
        }

        var previousStatus = workOrder.Status;
        workOrder.Status = WorkOrderStatus.Scheduled;
        workOrder.ApprovedById = approverUserId;
        workOrder.ApprovedAt = DateTime.UtcNow;
        workOrder.ApprovalComments = decision.Comments;
        workOrder.ConflictDetected = false;
        workOrder.UpdatedAt = DateTime.UtcNow;

        // Update Request status to Scheduled
        if (workOrder.Request != null)
        {
            workOrder.Request.Status = RequestStatus.Scheduled;
            workOrder.Request.UpdatedAt = DateTime.UtcNow;
        }

        // Update Workflow & ApprovalAction state
        var workflow = await _context.AgentWorkflows
            .Include(w => w.ApprovalActions)
            .FirstOrDefaultAsync(w => w.RequestId == workOrder.RequestId && w.Status == WorkflowStatus.WaitingForApproval);

        if (workflow != null)
        {
            workflow.Status = WorkflowStatus.Approved;
            workflow.CompletedAt = DateTime.UtcNow;
            foreach (var action in workflow.ApprovalActions.Where(a => a.Status == ApprovalStatus.Pending))
            {
                action.Status = ApprovalStatus.Approved;
                action.ApproverId = approverUserId;
                action.Comments = decision.Comments;
                action.DecidedAt = DateTime.UtcNow;
            }
        }

        // Add history
        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = previousStatus,
            NewStatus = WorkOrderStatus.Scheduled,
            ChangedById = approverUserId,
            Reason = $"Manager approval granted. Comments: {decision.Comments}"
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        // Send notification to technician
        if (workOrder.Technician?.UserId != null)
        {
            await _notificationService.SendNotificationAsync(
                workOrder.Technician.UserId,
                "New Work Order Assigned",
                $"Work Order {workOrder.WorkOrderNumber} ({workOrder.Title}) has been approved and scheduled for {workOrder.ScheduledStartTime:yyyy-MM-dd HH:mm}.",
                "Info");
        }

        await _auditLogService.LogAsync(
            approverUserId,
            "WorkOrderApproved",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrder.WorkOrderNumber, decision.Comments }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<WorkOrderDto> RejectWorkOrderAsync(Guid id, WorkOrderApprovalDecisionDto decision, Guid approverUserId)
    {
        var workOrder = await _context.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);
        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        var prev = workOrder.Status;
        workOrder.Status = WorkOrderStatus.Rejected;
        workOrder.ApprovalComments = decision.Comments;
        workOrder.UpdatedAt = DateTime.UtcNow;

        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = prev,
            NewStatus = WorkOrderStatus.Rejected,
            ChangedById = approverUserId,
            Reason = $"Manager rejected proposal: {decision.Comments}"
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            approverUserId,
            "WorkOrderRejected",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrder.WorkOrderNumber, decision.Comments }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<WorkOrderDto> RequestRevisionAsync(Guid id, WorkOrderApprovalDecisionDto decision, Guid approverUserId)
    {
        var workOrder = await _context.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);
        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        var prev = workOrder.Status;
        workOrder.Status = WorkOrderStatus.RevisionRequested;
        workOrder.ApprovalComments = decision.RevisionNotes;
        workOrder.UpdatedAt = DateTime.UtcNow;

        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = prev,
            NewStatus = WorkOrderStatus.RevisionRequested,
            ChangedById = approverUserId,
            Reason = $"Manager requested revision: {decision.RevisionNotes}"
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            approverUserId,
            "RevisionRequested",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrder.WorkOrderNumber, decision.RevisionNotes }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<WorkNoteDto> AddNoteAsync(Guid workOrderId, WorkNoteCreateDto dto, Guid authorUserId)
    {
        var workOrder = await _context.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == workOrderId && !w.IsDeleted);
        if (workOrder == null) throw new NotFoundException($"Work order '{workOrderId}' not found.");

        var user = await _context.Users.FindAsync(authorUserId);

        var note = new WorkNote
        {
            WorkOrderId = workOrderId,
            AuthorId = authorUserId,
            NoteText = dto.NoteText,
            Timestamp = DateTime.UtcNow
        };

        await _context.Set<WorkNote>().AddAsync(note);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            authorUserId,
            "WorkNoteAdded",
            "WorkOrder",
            workOrderId.ToString(),
            JsonSerializer.Serialize(new { note.Id, dto.NoteText }),
            "127.0.0.1");

        return new WorkNoteDto
        {
            Id = note.Id,
            WorkOrderId = note.WorkOrderId,
            AuthorId = note.AuthorId,
            AuthorName = user != null ? $"{user.FirstName} {user.LastName}" : "System User",
            NoteText = note.NoteText,
            Timestamp = note.Timestamp
        };
    }

    public async Task<WorkOrderDto> CompleteWorkOrderAsync(Guid id, CompleteWorkOrderDto dto, Guid technicianUserId)
    {
        var workOrder = await _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .Include(w => w.Technician)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        if (workOrder.Technician?.UserId != technicianUserId)
            throw new UnauthorizedAccessException("Only the assigned technician can complete this work order.");

        var prev = workOrder.Status;
        workOrder.Status = WorkOrderStatus.Completed;
        workOrder.ActualEndTime = DateTime.UtcNow;
        workOrder.UpdatedAt = DateTime.UtcNow;

        if (workOrder.Request != null)
        {
            workOrder.Request.Status = RequestStatus.Completed;
            workOrder.Request.UpdatedAt = DateTime.UtcNow;
        }

        // Add Completion Evidence
        var evidence = new CompletionEvidence
        {
            WorkOrderId = workOrder.Id,
            UploadedById = technicianUserId,
            SignerName = dto.SignerName,
            SignatureDataUrl = dto.SignatureDataUrl,
            FileKey = dto.PhotoFileKey ?? string.Empty,
            OriginalFileName = dto.PhotoOriginalFileName ?? string.Empty,
            Caption = dto.CompletionNotes ?? "Customer signed completion verification",
            UploadedAt = DateTime.UtcNow
        };
        await _context.Set<CompletionEvidence>().AddAsync(evidence);

        if (!string.IsNullOrWhiteSpace(dto.CompletionNotes))
        {
            var note = new WorkNote
            {
                WorkOrderId = workOrder.Id,
                AuthorId = technicianUserId,
                NoteText = $"Job Completed: {dto.CompletionNotes}",
                Timestamp = DateTime.UtcNow
            };
            await _context.Set<WorkNote>().AddAsync(note);
        }

        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = prev,
            NewStatus = WorkOrderStatus.Completed,
            ChangedById = technicianUserId,
            Reason = $"Work order marked complete by technician with customer sign-off from {dto.SignerName}."
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            technicianUserId,
            "WorkOrderCompleted",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrder.WorkOrderNumber, dto.SignerName }),
            "127.0.0.1");

        return await GetWorkOrderByIdAsync(workOrder.Id);
    }

    public async Task<List<WorkOrderSummaryDto>> GetPendingApprovalsAsync()
    {
        return await _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .Include(w => w.Location)
            .Where(w => !w.IsDeleted && (w.Status == WorkOrderStatus.PendingManagerApproval || w.Status == WorkOrderStatus.Proposed))
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.ScheduledStartTime)
            .Select(w => new WorkOrderSummaryDto
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                Title = w.Title,
                RequestId = w.RequestId,
                RequestNumber = w.Request != null ? w.Request.RequestNumber : string.Empty,
                TechnicianName = w.Technician != null && w.Technician.User != null ? $"{w.Technician.User.FirstName} {w.Technician.User.LastName}" : "Unassigned",
                LocationName = w.Location != null ? w.Location.Name : "Unassigned",
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                ScheduledStartTime = w.ScheduledStartTime,
                ScheduledEndTime = w.ScheduledEndTime,
                EstimatedDurationMinutes = w.EstimatedDurationMinutes,
                SLADeadline = w.SLADeadline,
                ConflictDetected = w.ConflictDetected,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<WorkOrderSummaryDto>> GetTechnicianScheduleAsync(Guid technicianUserId, DateTime? filterDate = null)
    {
        var tech = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == technicianUserId && !t.IsDeleted);
        if (tech == null) return new List<WorkOrderSummaryDto>();

        var query = _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .Include(w => w.Location)
            .Where(w => !w.IsDeleted && w.TechnicianId == tech.Id &&
                        w.Status != WorkOrderStatus.Draft &&
                        w.Status != WorkOrderStatus.Cancelled &&
                        w.Status != WorkOrderStatus.Rejected);

        if (filterDate.HasValue)
        {
            var date = filterDate.Value.Date;
            var nextDate = date.AddDays(1);
            query = query.Where(w => w.ScheduledStartTime.HasValue &&
                                     w.ScheduledStartTime.Value >= date &&
                                     w.ScheduledStartTime.Value < nextDate);
        }

        return await query
            .OrderBy(w => w.ScheduledStartTime)
            .Select(w => new WorkOrderSummaryDto
            {
                Id = w.Id,
                WorkOrderNumber = w.WorkOrderNumber,
                Title = w.Title,
                RequestId = w.RequestId,
                RequestNumber = w.Request != null ? w.Request.RequestNumber : string.Empty,
                TechnicianName = w.Technician != null && w.Technician.User != null ? $"{w.Technician.User.FirstName} {w.Technician.User.LastName}" : "Unassigned",
                LocationName = w.Location != null ? w.Location.Name : "Unassigned",
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                ScheduledStartTime = w.ScheduledStartTime,
                ScheduledEndTime = w.ScheduledEndTime,
                EstimatedDurationMinutes = w.EstimatedDurationMinutes,
                SLADeadline = w.SLADeadline,
                ConflictDetected = w.ConflictDetected,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    private static void ValidateStateTransition(WorkOrderStatus current, WorkOrderStatus target)
    {
        if (current == target) return;

        var allowed = current switch
        {
            WorkOrderStatus.Draft => new[] { WorkOrderStatus.Proposed, WorkOrderStatus.PendingManagerApproval, WorkOrderStatus.Scheduled, WorkOrderStatus.Cancelled },
            WorkOrderStatus.Proposed => new[] { WorkOrderStatus.PendingManagerApproval, WorkOrderStatus.Failed, WorkOrderStatus.Cancelled },
            WorkOrderStatus.PendingManagerApproval => new[] { WorkOrderStatus.Approved, WorkOrderStatus.Scheduled, WorkOrderStatus.Rejected, WorkOrderStatus.RevisionRequested, WorkOrderStatus.Cancelled },
            WorkOrderStatus.Approved => new[] { WorkOrderStatus.Scheduled, WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled },
            WorkOrderStatus.Scheduled => new[] { WorkOrderStatus.InProgress, WorkOrderStatus.Paused, WorkOrderStatus.Cancelled },
            WorkOrderStatus.InProgress => new[] { WorkOrderStatus.Paused, WorkOrderStatus.Completed, WorkOrderStatus.Cancelled },
            WorkOrderStatus.Paused => new[] { WorkOrderStatus.InProgress, WorkOrderStatus.Completed, WorkOrderStatus.Cancelled },
            WorkOrderStatus.RevisionRequested => new[] { WorkOrderStatus.Proposed, WorkOrderStatus.PendingManagerApproval, WorkOrderStatus.Cancelled },
            WorkOrderStatus.Rejected => new[] { WorkOrderStatus.Draft, WorkOrderStatus.Proposed, WorkOrderStatus.Cancelled },
            _ => Array.Empty<WorkOrderStatus>()
        };

        if (!allowed.Contains(target))
        {
            throw new InvalidOperationException($"Invalid status transition from '{current}' to '{target}'.");
        }
    }

    private static WorkOrderDto MapToDto(WorkOrder w)
    {
        return new WorkOrderDto
        {
            Id = w.Id,
            WorkOrderNumber = w.WorkOrderNumber,
            Title = w.Title,
            Description = w.Description,
            RequestId = w.RequestId,
            RequestNumber = w.Request?.RequestNumber ?? string.Empty,
            RequestTitle = w.Request?.Title ?? string.Empty,
            TechnicianId = w.TechnicianId,
            TechnicianName = w.Technician?.User != null ? $"{w.Technician.User.FirstName} {w.Technician.User.LastName}" : "Unassigned",
            TechnicianEmployeeId = w.Technician?.EmployeeId ?? string.Empty,
            TechnicianSpecialization = w.Technician?.Specialization ?? string.Empty,
            LocationId = w.LocationId,
            LocationName = w.Location?.Name ?? "Unassigned",
            Building = w.Location?.Building ?? string.Empty,
            Room = w.Location?.Room ?? string.Empty,
            Priority = w.Priority.ToString(),
            Status = w.Status.ToString(),
            ScheduledStartTime = w.ScheduledStartTime,
            ScheduledEndTime = w.ScheduledEndTime,
            EstimatedDurationMinutes = w.EstimatedDurationMinutes,
            ActualStartTime = w.ActualStartTime,
            ActualEndTime = w.ActualEndTime,
            SLADeadline = w.SLADeadline,
            ApprovedById = w.ApprovedById,
            ApprovedByName = w.ApprovedBy != null ? $"{w.ApprovedBy.FirstName} {w.ApprovedBy.LastName}" : null,
            ApprovedAt = w.ApprovedAt,
            ApprovalComments = w.ApprovalComments,
            ConflictDetected = w.ConflictDetected,
            ConflictDetailsJson = w.ConflictDetailsJson,
            AiDecisionSummary = w.AiDecisionSummary,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,
            StatusHistories = w.StatusHistories.OrderByDescending(h => h.Timestamp).Select(h => new WorkOrderStatusHistoryDto
            {
                Id = h.Id,
                PreviousStatus = h.PreviousStatus.ToString(),
                NewStatus = h.NewStatus.ToString(),
                ChangedByName = h.ChangedBy != null ? $"{h.ChangedBy.FirstName} {h.ChangedBy.LastName}" : "System",
                Reason = h.Reason,
                Timestamp = h.Timestamp
            }).ToList(),
            Notes = w.Notes.OrderByDescending(n => n.Timestamp).Select(n => new WorkNoteDto
            {
                Id = n.Id,
                WorkOrderId = n.WorkOrderId,
                AuthorId = n.AuthorId,
                AuthorName = n.Author != null ? $"{n.Author.FirstName} {n.Author.LastName}" : "User",
                NoteText = n.NoteText,
                Timestamp = n.Timestamp
            }).ToList(),
            Evidence = w.Evidence.OrderByDescending(e => e.UploadedAt).Select(e => new CompletionEvidenceDto
            {
                Id = e.Id,
                WorkOrderId = e.WorkOrderId,
                FileKey = e.FileKey,
                OriginalFileName = e.OriginalFileName,
                Caption = e.Caption,
                UploadedByName = e.UploadedBy != null ? $"{e.UploadedBy.FirstName} {e.UploadedBy.LastName}" : "Technician",
                SignatureDataUrl = e.SignatureDataUrl,
                SignerName = e.SignerName,
                UploadedAt = e.UploadedAt
            }).ToList()
        };
    }

    public async Task<List<MaintenanceRequestSummaryDto>> GetAvailableRequestsAsync()
    {
        return await _context.MaintenanceRequests
            .Include(r => r.Location)
            .Include(r => r.Category)
            .Include(r => r.Requester)
            .Where(r => r.Status != RequestStatus.Cancelled && r.Status != RequestStatus.Completed)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MaintenanceRequestSummaryDto
            {
                Id = r.Id,
                RequestNumber = r.RequestNumber,
                Title = r.Title,
                Description = r.Description,
                Status = r.Status.ToString(),
                LocationId = r.LocationId,
                LocationName = r.Location != null ? r.Location.Name : "Unassigned",
                Building = r.Location != null ? r.Location.Building : string.Empty,
                Priority = r.Category != null ? r.Category.DefaultPriority : "Medium",
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : "General",
                RequesterId = r.RequesterId,
                RequesterName = r.Requester != null ? $"{r.Requester.FirstName} {r.Requester.LastName}" : "Unknown",
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<TechnicianSummaryDto>> GetTechniciansAsync()
    {
        return await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .Select(t => new TechnicianSummaryDto
            {
                Id = t.Id,
                UserId = t.UserId,
                EmployeeId = t.EmployeeId,
                Name = t.User != null ? $"{t.User.FirstName} {t.User.LastName}" : "Technician",
                Specialization = t.Specialization,
                IsAvailable = t.IsAvailable,
                Skills = t.Skills.Select(s => s.Name).ToList()
            })
            .ToListAsync();
    }
}

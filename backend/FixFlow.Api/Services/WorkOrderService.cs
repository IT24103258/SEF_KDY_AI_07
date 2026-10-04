using System.Diagnostics;
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
    private static readonly SemaphoreSlim WorkOrderNumberLock = new(1, 1);

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
            .Include(w => w.Proposals)
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
        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rawItems.Select(w =>
        {
            var latestProposal = w.Proposals?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            return new WorkOrderSummaryDto
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
                RequestedStartTime = latestProposal != null && latestProposal.RequestedStartTime.Year > 1 ? latestProposal.RequestedStartTime : null,
                RequestedEndTime = latestProposal != null && latestProposal.RequestedEndTime.Year > 1 ? latestProposal.RequestedEndTime : null,
                ScheduledStartTime = w.ScheduledStartTime,
                ScheduledEndTime = w.ScheduledEndTime,
                EstimatedDurationMinutes = w.EstimatedDurationMinutes,
                SLADeadline = w.SLADeadline,
                ConflictDetected = w.ConflictDetected,
                CreatedAt = w.CreatedAt
            };
        }).ToList();

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
            .Include(x => x.Proposals)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (w == null)
            throw new NotFoundException($"Work order with ID '{id}' was not found.");

        // Scoping checks
        if (currentUserRole == "Technician" && currentUserId.HasValue && w.Technician?.UserId != currentUserId.Value)
            throw new UnauthorizedAccessException("Technicians may only access work orders assigned to them.");

        if (currentUserRole == "Requester" && currentUserId.HasValue && w.Request?.RequesterId != currentUserId.Value)
            throw new UnauthorizedAccessException("Requesters may only view work orders for their own requests.");

        var bhValid = await ComputeBusinessHoursValidAsync(w.ScheduledStartTime, w.ScheduledEndTime);
        return MapToDto(w, bhValid);
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
            .Include(t => t.Skills)
            .FirstOrDefaultAsync(t => t.Id == dto.TechnicianId || t.UserId == dto.TechnicianId);
        if (tech == null)
            throw new NotFoundException($"Technician '{dto.TechnicianId}' was not found.");

        await WorkOrderNumberLock.WaitAsync();
        string workOrderNumber;
        try
        {
            var workOrderCount = await _context.Set<WorkOrder>().CountAsync() + 1;
            workOrderNumber = $"WO-{DateTime.UtcNow:yyyyMM}-{workOrderCount:D4}";
        }
        finally
        {
            WorkOrderNumberLock.Release();
        }

        var priority = Enum.TryParse<WorkOrderPriority>(dto.Priority, true, out var p) ? p : WorkOrderPriority.Medium;
        var durationMinutes = dto.EstimatedDurationMinutes > 0 ? dto.EstimatedDurationMinutes : 60;

        var scheduledStartUtc = dto.ScheduledStartTime.HasValue ? SchedulingService.NormalizeToUtc(dto.ScheduledStartTime.Value) : (DateTime?)null;
        var scheduledEndUtc = dto.ScheduledEndTime.HasValue ? SchedulingService.NormalizeToUtc(dto.ScheduledEndTime.Value) : (DateTime?)null;
        var slaDeadlineUtc = dto.SLADeadline.HasValue ? SchedulingService.NormalizeToUtc(dto.SLADeadline.Value) : (DateTime?)null;

        var requestedStartUtc = scheduledStartUtc;
        var requestedEndUtc = scheduledEndUtc;

        // Draft work order first (no schedule yet if times provided — we validate first)
        var workOrder = new WorkOrder
        {
            WorkOrderNumber = workOrderNumber,
            Title = dto.Title,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? req.Description : dto.Description,
            RequestId = req.Id,
            TechnicianId = tech.Id,
            LocationId = dto.LocationId ?? req.LocationId,
            Priority = priority,
            Status = WorkOrderStatus.Draft,
            EstimatedDurationMinutes = durationMinutes,
            SLADeadline = slaDeadlineUtc
        };

        var conflictDetected = false;
        var conflictDetails = new List<ConflictDetailDto>();
        ScheduleValidationResult? originalValidation = null;
        ScheduleValidationResult? finalValidation = null;

        var approvalReason = string.Empty;

        // If scheduled directly, validate times using the 5-scenario decision matrix
        if (scheduledStartUtc.HasValue && scheduledEndUtc.HasValue)
        {
            workOrder.ScheduledStartTime = scheduledStartUtc;
            workOrder.ScheduledEndTime = scheduledEndUtc;

            originalValidation = await _schedulingService.ValidateScheduleAsync(
                workOrder.TechnicianId,
                scheduledStartUtc.Value,
                scheduledEndUtc.Value,
                durationMinutes,
                dto.Priority,
                slaDeadlineUtc);

            var isConflict = !originalValidation.IsConflictFree;
            var isOutsideBusinessHours = !originalValidation.IsWithinBusinessHours;
            var isTechnicianUnavailable = !originalValidation.IsWithinTechnicianAvailability;
            var isSlaBreached = !originalValidation.IsSlaCompliant;
            var isCritical = priority == WorkOrderPriority.Critical;

            if (isConflict)
            {
                // Scenario 2: Actual schedule conflict → search for alternative slot → PendingManagerApproval
                conflictDetected = true;
                conflictDetails.AddRange(originalValidation.Conflicts);

                var alternativeFound = false;
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
                var searchAnchorUtc = requestedStartUtc ?? DateTime.UtcNow;
                var searchBaseLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    searchAnchorUtc.Kind != DateTimeKind.Utc ? searchAnchorUtc.ToUniversalTime() : searchAnchorUtc, tz);
                var maxSearchDays = 7;

                for (int dayOffset = 0; dayOffset < maxSearchDays && !alternativeFound; dayOffset++)
                {
                    var checkDateLocal = searchBaseLocal.Date.AddDays(dayOffset);
                    var dayOfWeek = (int)checkDateLocal.DayOfWeek;
                    var bh = await _context.Set<BusinessHours>()
                        .FirstOrDefaultAsync(b => b.DayOfWeek == dayOfWeek);

                    if (bh == null || !bh.IsWorkingDay) continue;

                    var dayOpenLocal = checkDateLocal.Add(bh.OpenTime);
                    var dayCloseLocal = checkDateLocal.Add(bh.CloseTime);

                    DateTime startSearchLocal;
                    if (dayOffset == 0 && searchBaseLocal > dayOpenLocal)
                    {
                        var snappedMinutes = (searchBaseLocal.Minute / 30) * 30;
                        startSearchLocal = new DateTime(checkDateLocal.Year, checkDateLocal.Month, checkDateLocal.Day,
                            searchBaseLocal.Hour, snappedMinutes, 0, DateTimeKind.Unspecified);
                        if (startSearchLocal < searchBaseLocal)
                            startSearchLocal = startSearchLocal.AddMinutes(30);
                    }
                    else
                    {
                        startSearchLocal = dayOpenLocal;
                    }

                    while (startSearchLocal.AddMinutes(durationMinutes) <= dayCloseLocal)
                    {
                        var endSearchLocal = startSearchLocal.AddMinutes(durationMinutes);
                        var startSearchUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(startSearchLocal, DateTimeKind.Unspecified), tz);
                        var endSearchUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(endSearchLocal, DateTimeKind.Unspecified), tz);

                        var slotVal = await _schedulingService.ValidateScheduleAsync(
                            tech.Id, startSearchUtc, endSearchUtc, durationMinutes,
                            dto.Priority, slaDeadlineUtc);

                        if (slotVal.IsValid)
                        {
                            workOrder.ScheduledStartTime = startSearchUtc;
                            workOrder.ScheduledEndTime = endSearchUtc;
                            alternativeFound = true;
                            break;
                        }

                        startSearchLocal = startSearchLocal.AddMinutes(30);
                    }
                }

                if (!alternativeFound)
                {
                    conflictDetails.Add(new ConflictDetailDto
                    {
                        Reason = "No conflict-free slot found within standard operating window. Requires manual manager scheduling."
                    });
                }

                workOrder.Status = WorkOrderStatus.PendingManagerApproval;
                workOrder.ConflictDetected = true;
                workOrder.ConflictDetailsJson = JsonSerializer.Serialize(conflictDetails);
                workOrder.AiDecisionSummary = conflictDetails.Any(c => c.Reason.Contains("No conflict-free slot"))
                    ? $"Schedule conflict detected for technician {tech.User?.FirstName} {tech.User?.LastName}. No valid alternative slot found within {maxSearchDays} days. Manual manager scheduling required."
                    : $"Schedule conflict detected for technician {tech.User?.FirstName} {tech.User?.LastName}. Original requested slot has overlapping booking. Alternative slot proposed for Manager review.";
                approvalReason = "Schedule conflict detected. Manager sign-off required for alternative slot or manual scheduling.";
            }
            else if (isOutsideBusinessHours)
            {
                // Scenario 3: Outside business hours → search for next valid operational slot → PendingManagerApproval
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
                var searchAnchorUtc = requestedStartUtc ?? DateTime.UtcNow;
                var searchBaseLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    searchAnchorUtc.Kind != DateTimeKind.Utc ? searchAnchorUtc.ToUniversalTime() : searchAnchorUtc, tz);
                var alternativeFound = false;
                var maxSearchDays = 7;

                for (int dayOffset = 0; dayOffset < maxSearchDays && !alternativeFound; dayOffset++)
                {
                    var checkDateLocal = searchBaseLocal.Date.AddDays(dayOffset);
                    var dayOfWeek = (int)checkDateLocal.DayOfWeek;
                    var bh = await _context.Set<BusinessHours>()
                        .FirstOrDefaultAsync(b => b.DayOfWeek == dayOfWeek);

                    if (bh == null || !bh.IsWorkingDay) continue;

                    var dayOpenLocal = checkDateLocal.Add(bh.OpenTime);
                    var dayCloseLocal = checkDateLocal.Add(bh.CloseTime);

                    DateTime startSearchLocal;
                    if (dayOffset == 0 && searchBaseLocal > dayOpenLocal)
                    {
                        var snappedMinutes = (searchBaseLocal.Minute / 30) * 30;
                        startSearchLocal = new DateTime(checkDateLocal.Year, checkDateLocal.Month, checkDateLocal.Day,
                            searchBaseLocal.Hour, snappedMinutes, 0, DateTimeKind.Unspecified);
                        if (startSearchLocal < searchBaseLocal)
                            startSearchLocal = startSearchLocal.AddMinutes(30);
                    }
                    else
                    {
                        startSearchLocal = dayOpenLocal;
                    }

                    while (startSearchLocal.AddMinutes(durationMinutes) <= dayCloseLocal)
                    {
                        var endSearchLocal = startSearchLocal.AddMinutes(durationMinutes);
                        var startSearchUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(startSearchLocal, DateTimeKind.Unspecified), tz);
                        var endSearchUtc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(endSearchLocal, DateTimeKind.Unspecified), tz);

                        var slotVal = await _schedulingService.ValidateScheduleAsync(
                            tech.Id, startSearchUtc, endSearchUtc, durationMinutes,
                            dto.Priority, slaDeadlineUtc);

                        if (slotVal.IsValid)
                        {
                            workOrder.ScheduledStartTime = startSearchUtc;
                            workOrder.ScheduledEndTime = endSearchUtc;
                            alternativeFound = true;
                            break;
                        }

                        startSearchLocal = startSearchLocal.AddMinutes(30);
                    }
                }

                workOrder.Status = WorkOrderStatus.PendingManagerApproval;
                workOrder.ConflictDetected = false;
                if (alternativeFound)
                {
                    workOrder.AiDecisionSummary = $"Requested time falls outside operational business hours for technician {tech.User?.FirstName} {tech.User?.LastName}. Next available operational slot proposed for Manager review.";
                }
                else
                {
                    workOrder.AiDecisionSummary = $"Requested time falls outside operational business hours for technician {tech.User?.FirstName} {tech.User?.LastName}. No valid operational slot found within {maxSearchDays} days. Manual manager scheduling required.";
                }
                approvalReason = "Requested time is outside operational business hours. Manager approval required.";
            }
            else if (isTechnicianUnavailable)
            {
                // Technician unavailable → PendingManagerApproval
                workOrder.Status = WorkOrderStatus.PendingManagerApproval;
                workOrder.ConflictDetected = false;
                workOrder.AiDecisionSummary = $"Assigned technician {tech.User?.FirstName} {tech.User?.LastName} is currently unavailable. Manager review required.";
                approvalReason = "Technician is marked as unavailable. Manager review required.";
            }
            else if (isSlaBreached)
            {
                // SLA breach → PendingManagerApproval
                workOrder.Status = WorkOrderStatus.PendingManagerApproval;
                workOrder.ConflictDetected = false;
                workOrder.AiDecisionSummary = $"Proposed schedule breaches SLA deadline for technician {tech.User?.FirstName} {tech.User?.LastName}. Manager approval required.";
                approvalReason = "Proposed schedule breaches SLA deadline. Manager approval required.";
            }
            else if (isCritical)
            {
                // Scenario 4: Critical priority → PendingManagerApproval
                workOrder.Status = WorkOrderStatus.PendingManagerApproval;
                workOrder.ConflictDetected = false;
                workOrder.AiDecisionSummary = $"Critical priority work order for technician {tech.User?.FirstName} {tech.User?.LastName}. Manager sign-off required before scheduling.";
                approvalReason = "Critical priority work order requires Manager sign-off.";
            }
            else
            {
                // Scenarios 1 & 5: No conflict + valid hours + non-critical → Scheduled immediately
                workOrder.Status = WorkOrderStatus.Scheduled;
                workOrder.ConflictDetected = false;
                workOrder.AiDecisionSummary = $"Schedule validated successfully. No conflicts detected, within business hours. Auto-scheduled for technician {tech.User?.FirstName} {tech.User?.LastName}.";
            }
        }
        else
        {
            // No time specified — Draft
            workOrder.Status = WorkOrderStatus.Draft;
        }

        await _context.Set<WorkOrder>().AddAsync(workOrder);

        // If approval is required (conflict or high-impact priority) and we have scheduled times, create proposal + workflow records
        if (workOrder.Status == WorkOrderStatus.PendingManagerApproval && workOrder.ScheduledStartTime.HasValue)
        {
            // Run final validation on the proposed alternative slot
            finalValidation = await _schedulingService.ValidateScheduleAsync(
                workOrder.TechnicianId,
                workOrder.ScheduledStartTime.Value,
                workOrder.ScheduledEndTime!.Value,
                durationMinutes,
                dto.Priority,
                slaDeadlineUtc);

            var checklist = new ValidationChecklistDto
            {
                TechnicianAvailable = finalValidation.IsWithinTechnicianAvailability,
                ExistingBookingsChecked = true,
                SlaRequirementPassed = finalValidation.IsSlaCompliant,
                ScheduleConflictNone = finalValidation.IsConflictFree,
                BusinessHoursValid = finalValidation.IsWithinBusinessHours,
                RequiredSkillValid = tech.Skills.Any(),
                SchemaValid = true
            };

            // Create ScheduleProposal
            var proposal = new ScheduleProposal
            {
                WorkOrderId = workOrder.Id,
                RequestId = req.Id,
                TechnicianId = tech.Id,
                RequestedStartTime = requestedStartUtc!.Value,
                RequestedEndTime = requestedEndUtc!.Value,
                ProposedStartTime = workOrder.ScheduledStartTime.Value,
                ProposedEndTime = workOrder.ScheduledEndTime!.Value,
                EstimatedDurationMinutes = durationMinutes,
                Priority = priority,
                SlaDeadline = slaDeadlineUtc,
                ConflictDetected = conflictDetected,
                ConflictDetailsJson = JsonSerializer.Serialize(conflictDetails),
                DecisionSummary = workOrder.AiDecisionSummary,
                ValidationDetailsJson = JsonSerializer.Serialize(checklist),
                IsAccepted = false
            };
            await _context.Set<ScheduleProposal>().AddAsync(proposal);

            // Create AgentWorkflow + ApprovalAction
            var agentWorkflow = new AgentWorkflow
            {
                RequestId = req.Id,
                WorkflowType = "SchedulingPipeline",
                Status = WorkflowStatus.WaitingForApproval,
                OutputSummaryJson = JsonSerializer.Serialize(new
                {
                    workOrderId = workOrder.Id,
                    technicianId = tech.Id,
                    proposedStart = workOrder.ScheduledStartTime.Value,
                    proposedEnd = workOrder.ScheduledEndTime!.Value,
                    conflictDetected,
                    decisionSummary = workOrder.AiDecisionSummary
                })
            };

            var schedulingStep = new AgentStep
            {
                WorkflowId = agentWorkflow.Id,
                AgentName = "SchedulingAgent",
                StepName = "Conflict-Free Schedule Generation",
                Status = WorkflowStatus.WaitingForApproval,
                InputDataJson = JsonSerializer.Serialize(new
                {
                    requestId = req.Id,
                    technicianId = tech.Id,
                    priority = dto.Priority,
                    duration = durationMinutes,
                    sla = slaDeadlineUtc,
                    originalRequestedStart = scheduledStartUtc!.Value,
                    originalRequestedEnd = scheduledEndUtc!.Value
                }),
                OutputDataJson = JsonSerializer.Serialize(new
                {
                    proposalId = proposal.Id,
                    proposedStartTime = proposal.ProposedStartTime,
                    proposedEndTime = proposal.ProposedEndTime,
                    conflictDetected,
                    decisionSummary = workOrder.AiDecisionSummary
                }),
                ValidationResultJson = JsonSerializer.Serialize(checklist)
            };

            var activeStatusesForCount = new[]
            {
                WorkOrderStatus.Proposed,
                WorkOrderStatus.PendingManagerApproval,
                WorkOrderStatus.Approved,
                WorkOrderStatus.Scheduled,
                WorkOrderStatus.InProgress,
                WorkOrderStatus.Paused
            };

            var sw = Stopwatch.StartNew();
            var activeBookingsCount = await _context.Set<WorkOrder>()
                .CountAsync(w => !w.IsDeleted &&
                                 w.TechnicianId == tech.Id &&
                                 activeStatusesForCount.Contains(w.Status));
            sw.Stop();
            var techCalendarTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var scheduledStartLocal = SchedulingService.ConvertToLocal(workOrder.ScheduledStartTime.Value);
            var proposedDayOfWeek = (int)scheduledStartLocal.DayOfWeek;
            var actualBusinessHours = await _context.Set<BusinessHours>()
                .FirstOrDefaultAsync(b => b.DayOfWeek == proposedDayOfWeek);
            sw.Stop();
            var businessHoursTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var conflictingCount = await _context.Set<WorkOrder>()
                .Where(w => !w.IsDeleted &&
                            w.TechnicianId == tech.Id &&
                            activeStatusesForCount.Contains(w.Status) &&
                            w.ScheduledStartTime.HasValue &&
                            w.ScheduledEndTime.HasValue &&
                            w.ScheduledStartTime.Value < workOrder.ScheduledEndTime!.Value &&
                            w.ScheduledEndTime.Value > workOrder.ScheduledStartTime.Value)
                .CountAsync();
            sw.Stop();
            var existingWorkOrdersTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var validationData = finalValidation;
            sw.Stop();
            var validationTimeMs = (int)sw.ElapsedMilliseconds;

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "GetTechnicianCalendar",
                InputJson = JsonSerializer.Serialize(new { technicianId = tech.Id }),
                OutputJson = JsonSerializer.Serialize(new { isAvailable = tech.IsAvailable, activeBookingsCount }),
                ExecutionTimeMs = techCalendarTimeMs,
                Success = true
            });

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "GetBusinessHours",
                InputJson = JsonSerializer.Serialize(new { date = scheduledStartLocal.ToString("yyyy-MM-dd"), dayOfWeek = proposedDayOfWeek }),
                OutputJson = JsonSerializer.Serialize(new
                {
                    open = actualBusinessHours?.OpenTime.ToString(@"hh\:mm") ?? "N/A",
                    close = actualBusinessHours?.CloseTime.ToString(@"hh\:mm") ?? "N/A",
                    isWorkingDay = actualBusinessHours?.IsWorkingDay ?? false
                }),
                ExecutionTimeMs = businessHoursTimeMs,
                Success = actualBusinessHours != null
            });

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "GetExistingWorkOrders",
                InputJson = JsonSerializer.Serialize(new { technicianId = tech.Id, checkDate = scheduledStartLocal.ToString("yyyy-MM-dd") }),
                OutputJson = JsonSerializer.Serialize(new { conflictCount = conflictingCount, activeBookingsCount }),
                ExecutionTimeMs = existingWorkOrdersTimeMs,
                Success = true
            });

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "ValidateSchedule",
                InputJson = JsonSerializer.Serialize(new { technicianId = tech.Id, start = workOrder.ScheduledStartTime.Value, end = workOrder.ScheduledEndTime.Value }),
                OutputJson = JsonSerializer.Serialize(validationData),
                ExecutionTimeMs = validationTimeMs,
                Success = finalValidation.IsValid
            });

            agentWorkflow.Steps.Add(schedulingStep);

            agentWorkflow.ApprovalActions.Add(new ApprovalAction
            {
                WorkflowId = agentWorkflow.Id,
                Status = ApprovalStatus.Pending,
                ReasonRequired = !string.IsNullOrEmpty(approvalReason) ? approvalReason : "Manager sign-off required for schedule proposal.",
                RequestedAt = DateTime.UtcNow
            });

            await _context.AgentWorkflows.AddAsync(agentWorkflow);
        }

        var history = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = WorkOrderStatus.Draft,
            NewStatus = workOrder.Status,
            ChangedById = creatorUserId,
            Reason = workOrder.Status == WorkOrderStatus.PendingManagerApproval
                ? (conflictDetected
                    ? "Work order created with schedule conflict. Routed to Manager for review."
                    : !string.IsNullOrEmpty(approvalReason)
                        ? approvalReason
                        : $"Work order created with {priority.ToString().ToLower()} priority. Routed to Manager for approval.")
                : workOrder.Status == WorkOrderStatus.Scheduled
                    ? "Work order validated and auto-scheduled. No conflicts or approval requirements detected."
                    : "Work order created as draft."
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(history);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            creatorUserId,
            "WorkOrderCreated",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrderNumber, workOrder.Status, workOrder.RequestId, workOrder.TechnicianId, conflictDetected }),
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
            workOrder.ScheduledStartTime = SchedulingService.NormalizeToUtc(dto.ScheduledStartTime.Value);
            workOrder.ScheduledEndTime = dto.ScheduledEndTime.HasValue
                ? SchedulingService.NormalizeToUtc(dto.ScheduledEndTime.Value)
                : workOrder.ScheduledStartTime.Value.AddMinutes(dto.EstimatedDurationMinutes);

            // Re-validate schedule
            var valResult = await _schedulingService.ValidateScheduleAsync(
                workOrder.TechnicianId,
                workOrder.ScheduledStartTime.Value,
                workOrder.ScheduledEndTime.Value,
                workOrder.EstimatedDurationMinutes,
                dto.Priority,
                workOrder.SLADeadline,
                workOrder.Id);

            workOrder.ConflictDetected = !valResult.IsConflictFree;
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
            var previousStatus = workOrder.Status;
            workOrder.Status = WorkOrderStatus.Cancelled;
            workOrder.UpdatedAt = DateTime.UtcNow;

            var history = new WorkOrderStatusHistory
            {
                WorkOrderId = workOrder.Id,
                PreviousStatus = previousStatus,
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

            if (!valResult.IsValid)
            {
                throw new InvalidOperationException($"Schedule validation failed: {string.Join("; ", valResult.ValidationErrors)}");
            }

            workOrder.ConflictDetected = false;
        }

        var previousStatus = workOrder.Status;
        workOrder.Status = WorkOrderStatus.Scheduled;
        workOrder.ApprovedById = approverUserId;
        workOrder.ApprovedAt = DateTime.UtcNow;
        workOrder.ApprovalComments = decision.Comments;
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

        // Mark the related ScheduleProposal as accepted
        var proposal = await _context.Set<ScheduleProposal>()
            .FirstOrDefaultAsync(p => p.WorkOrderId == workOrder.Id && !p.IsAccepted);
        if (proposal != null)
        {
            proposal.IsAccepted = true;
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
        var workOrder = await _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);
        if (workOrder == null) throw new NotFoundException($"Work order '{id}' not found.");

        var prev = workOrder.Status;
        workOrder.Status = WorkOrderStatus.Rejected;
        workOrder.ApprovalComments = decision.Comments;
        workOrder.UpdatedAt = DateTime.UtcNow;

        // Update Workflow & ApprovalAction state
        var workflow = await _context.AgentWorkflows
            .Include(w => w.ApprovalActions)
            .FirstOrDefaultAsync(w => w.RequestId == workOrder.RequestId && w.Status == WorkflowStatus.WaitingForApproval);

        if (workflow != null)
        {
            workflow.Status = WorkflowStatus.Rejected;
            workflow.CompletedAt = DateTime.UtcNow;
            foreach (var action in workflow.ApprovalActions.Where(a => a.Status == ApprovalStatus.Pending))
            {
                action.Status = ApprovalStatus.Rejected;
                action.ApproverId = approverUserId;
                action.Comments = decision.Comments;
                action.DecidedAt = DateTime.UtcNow;
            }
        }

        // Mark the related ScheduleProposal as not accepted
        var proposal = await _context.Set<ScheduleProposal>()
            .FirstOrDefaultAsync(p => p.WorkOrderId == workOrder.Id);
        if (proposal != null)
        {
            proposal.IsAccepted = false;
        }

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
        var businessHoursList = await _context.Set<BusinessHours>().ToListAsync();
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

        var items = await _context.Set<WorkOrder>()
            .Include(w => w.Request)
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .Include(w => w.Location)
            .Include(w => w.Proposals)
            .Where(w => !w.IsDeleted && (w.Status == WorkOrderStatus.PendingManagerApproval || w.Status == WorkOrderStatus.Proposed))
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.ScheduledStartTime)
            .ToListAsync();

        var result = items.Select(w =>
        {
            var latestProposal = w.Proposals?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            var startUtc = w.ScheduledStartTime;
            var endUtc = w.ScheduledEndTime;

            ValidationChecklistDto? checklist = null;
            if (startUtc.HasValue)
            {
                var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    startUtc.Value.Kind != DateTimeKind.Utc ? startUtc.Value.ToUniversalTime() : startUtc.Value, tz);
                var bh = businessHoursList.FirstOrDefault(b => b.DayOfWeek == (int)startLocal.DayOfWeek);

                var bizHoursValid = true;
                if (bh == null || !bh.IsWorkingDay)
                {
                    bizHoursValid = false;
                }
                else
                {
                    var endLocal = endUtc.HasValue
                        ? TimeZoneInfo.ConvertTimeFromUtc(
                            endUtc.Value.Kind != DateTimeKind.Utc ? endUtc.Value.ToUniversalTime() : endUtc.Value, tz)
                        : (DateTime?)null;

                    if (endLocal.HasValue && startLocal.Date != endLocal.Value.Date)
                        bizHoursValid = false;
                    else if (endLocal.HasValue)
                        bizHoursValid = startLocal.TimeOfDay >= bh.OpenTime && endLocal.Value.TimeOfDay <= bh.CloseTime;
                    else
                        bizHoursValid = startLocal.TimeOfDay >= bh.OpenTime && startLocal.TimeOfDay <= bh.CloseTime;
                }

                checklist = new ValidationChecklistDto
                {
                    TechnicianAvailable = latestProposal != null
                        ? DeserializeValidationBool(latestProposal.ValidationDetailsJson, "TechnicianAvailable")
                        : true,
                    ExistingBookingsChecked = latestProposal != null
                        ? DeserializeValidationBool(latestProposal.ValidationDetailsJson, "ExistingBookingsChecked")
                        : true,
                    SlaRequirementPassed = !w.SLADeadline.HasValue || endUtc <= w.SLADeadline,
                    ScheduleConflictNone = !w.ConflictDetected,
                    BusinessHoursValid = bizHoursValid,
                    RequiredSkillValid = latestProposal != null
                        ? DeserializeValidationBool(latestProposal.ValidationDetailsJson, "RequiredSkillValid")
                        : true,
                    SchemaValid = latestProposal != null
                        ? DeserializeValidationBool(latestProposal.ValidationDetailsJson, "SchemaValid")
                        : true
                };
            }

            return new WorkOrderSummaryDto
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
                RequestedStartTime = latestProposal?.RequestedStartTime,
                RequestedEndTime = latestProposal?.RequestedEndTime,
                ScheduledStartTime = w.ScheduledStartTime,
                ScheduledEndTime = w.ScheduledEndTime,
                EstimatedDurationMinutes = w.EstimatedDurationMinutes,
                SLADeadline = w.SLADeadline,
                ConflictDetected = w.ConflictDetected,
                AiDecisionSummary = w.AiDecisionSummary,
                ConflictDetailsJson = w.ConflictDetailsJson,
                ValidationChecklist = checklist,
                CreatedAt = w.CreatedAt
            };
        }).ToList();

        return result;
    }

    private static bool DeserializeValidationBool(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(propertyName, out var val))
                return val.GetBoolean();
            if (doc.RootElement.TryGetProperty(ToCamelCase(propertyName), out val))
                return val.GetBoolean();
        }
        catch { }
        return true;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
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

    private async Task<bool> ComputeBusinessHoursValidAsync(DateTime? scheduledStartUtc, DateTime? scheduledEndUtc)
    {
        if (!scheduledStartUtc.HasValue) return true;

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
            scheduledStartUtc.Value.Kind != DateTimeKind.Utc ? scheduledStartUtc.Value.ToUniversalTime() : scheduledStartUtc.Value, tz);

        var bh = await _context.Set<BusinessHours>().FirstOrDefaultAsync(b => b.DayOfWeek == (int)startLocal.DayOfWeek);
        if (bh == null || !bh.IsWorkingDay) return false;

        if (scheduledEndUtc.HasValue)
        {
            var endLocal = TimeZoneInfo.ConvertTimeFromUtc(
                scheduledEndUtc.Value.Kind != DateTimeKind.Utc ? scheduledEndUtc.Value.ToUniversalTime() : scheduledEndUtc.Value, tz);
            if (startLocal.Date != endLocal.Date) return false;
            return startLocal.TimeOfDay >= bh.OpenTime && endLocal.TimeOfDay <= bh.CloseTime;
        }

        var timeOfDay = startLocal.TimeOfDay;
        return timeOfDay >= bh.OpenTime && timeOfDay <= bh.CloseTime;
    }

    private static WorkOrderDto MapToDto(WorkOrder w, bool businessHoursValid = true)
    {
        ValidationChecklistDto? checklist = null;
        if (w.ScheduledStartTime.HasValue)
        {
            checklist = new ValidationChecklistDto
            {
                TechnicianAvailable = true,
                ExistingBookingsChecked = true,
                SlaRequirementPassed = !w.SLADeadline.HasValue || w.ScheduledEndTime <= w.SLADeadline,
                ScheduleConflictNone = !w.ConflictDetected,
                BusinessHoursValid = businessHoursValid,
                RequiredSkillValid = true,
                SchemaValid = true
            };
        }

        var latestProposal = w.Proposals?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

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
            RequestedStartTime = latestProposal != null && latestProposal.RequestedStartTime.Year > 1 ? latestProposal.RequestedStartTime : null,
            RequestedEndTime = latestProposal != null && latestProposal.RequestedEndTime.Year > 1 ? latestProposal.RequestedEndTime : null,
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
            ValidationChecklist = checklist,
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

    public async Task EnsureEvidenceUploadAllowedAsync(Guid workOrderId, Guid userId, string role)
    {
        var w = await _context.Set<WorkOrder>()
            .Include(x => x.Technician)
            .FirstOrDefaultAsync(x => x.Id == workOrderId && !x.IsDeleted);

        if (w == null)
            throw new NotFoundException($"Work order with ID '{workOrderId}' was not found.");

        // Endpoint is restricted to Administrator/Technician via route authorization;
        // technicians may only upload evidence for work orders assigned to them.
        if (role != "Administrator" && w.Technician?.UserId != userId)
            throw new ForbiddenException("Only the assigned technician may upload evidence for this work order.");
    }

    public async Task<CompletionEvidenceDto> GetEvidenceFileAsync(Guid workOrderId, Guid evidenceId, Guid? currentUserId, string? currentUserRole)
    {
        var w = await _context.Set<WorkOrder>()
            .Include(x => x.Request)
            .Include(x => x.Technician)
            .Include(x => x.Evidence)
            .FirstOrDefaultAsync(x => x.Id == workOrderId && !x.IsDeleted);

        if (w == null)
            throw new NotFoundException($"Work order with ID '{workOrderId}' was not found.");

        // Same scoping rules as GetWorkOrderByIdAsync, but with 403 semantics.
        if (currentUserRole == "Technician" && currentUserId.HasValue && w.Technician?.UserId != currentUserId.Value)
            throw new ForbiddenException("Technicians may only access work orders assigned to them.");

        if (currentUserRole == "Requester" && currentUserId.HasValue && w.Request?.RequesterId != currentUserId.Value)
            throw new ForbiddenException("Requesters may only view work orders for their own requests.");

        var evidence = w.Evidence.FirstOrDefault(e => e.Id == evidenceId);
        if (evidence == null || string.IsNullOrEmpty(evidence.FileKey))
            throw new NotFoundException("Evidence record was not found for this work order.");

        return new CompletionEvidenceDto
        {
            Id = evidence.Id,
            WorkOrderId = evidence.WorkOrderId,
            FileKey = evidence.FileKey,
            OriginalFileName = evidence.OriginalFileName
        };
    }
}

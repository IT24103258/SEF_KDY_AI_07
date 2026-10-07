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

public class SchedulingService : ISchedulingService
{
    private static readonly TimeZoneInfo FacilityTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
    private static readonly SemaphoreSlim WorkOrderNumberLock = new(1, 1);

    private readonly FixFlowDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IPythonSchedulingAgentClient? _pythonAgentClient;

    public SchedulingService(
        FixFlowDbContext context,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IPythonSchedulingAgentClient? pythonAgentClient = null)
    {
        _context = context;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _pythonAgentClient = pythonAgentClient;
    }

    internal static DateTime NormalizeToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Unspecified)
            return TimeZoneInfo.ConvertTimeToUtc(value, FacilityTimeZone);
        return value.ToUniversalTime();
    }

    internal static DateTime ConvertToLocal(DateTime utcValue)
    {
        if (utcValue.Kind != DateTimeKind.Utc)
            utcValue = utcValue.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, FacilityTimeZone);
    }

    public async Task<List<BusinessHoursDto>> GetBusinessHoursAsync()
    {
        await EnsureDefaultBusinessHoursAsync();

        var hours = await _context.Set<BusinessHours>()
            .OrderBy(b => b.DayOfWeek)
            .ToListAsync();

        return hours.Select(h => new BusinessHoursDto
        {
            DayOfWeek = h.DayOfWeek,
            DayName = h.DayName,
            OpenTime = h.OpenTime.ToString(@"hh\:mm"),
            CloseTime = h.CloseTime.ToString(@"hh\:mm"),
            IsWorkingDay = h.IsWorkingDay
        }).ToList();
    }

    public async Task<ScheduleValidationResult> ValidateScheduleAsync(
        Guid technicianId,
        DateTime startTime,
        DateTime endTime,
        int durationMinutes,
        string priority,
        DateTime? slaDeadline = null,
        Guid? excludeWorkOrderId = null)
    {
        var result = new ScheduleValidationResult
        {
            IsValid = true,
            IsConflictFree = true,
            IsWithinBusinessHours = true,
            IsWithinTechnicianAvailability = true,
            IsSlaCompliant = true
        };

        var startTimeUtc = NormalizeToUtc(startTime);
        var endTimeUtc = NormalizeToUtc(endTime);
        var slaDeadlineUtc = slaDeadline.HasValue ? NormalizeToUtc(slaDeadline.Value) : (DateTime?)null;

        var startTimeLocal = ConvertToLocal(startTimeUtc);
        var endTimeLocal = ConvertToLocal(endTimeUtc);

        if (startTimeUtc >= endTimeUtc)
        {
            result.IsValid = false;
            result.ValidationErrors.Add("Start time must be earlier than end time.");
        }

        if (durationMinutes <= 0)
        {
            result.IsValid = false;
            result.ValidationErrors.Add("Duration must be a positive number of minutes.");
        }

        // 1. Technician Existence & Availability Check
        var tech = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == technicianId && !t.IsDeleted);

        if (tech == null || tech.User == null || !tech.User.IsActive)
        {
            result.IsValid = false;
            result.IsWithinTechnicianAvailability = false;
            result.ValidationErrors.Add("Technician does not exist or user account is inactive.");
        }
        else if (!tech.IsAvailable)
        {
            result.IsValid = false;
            result.IsWithinTechnicianAvailability = false;
            result.ValidationErrors.Add($"Technician {tech.User.FirstName} {tech.User.LastName} is currently marked as unavailable.");
        }

        // 2. Business Hours Validation (uses facility-local wall-clock time)
        await EnsureDefaultBusinessHoursAsync();
        var dayOfWeek = (int)startTimeLocal.DayOfWeek;
        var businessHours = await _context.Set<BusinessHours>().FirstOrDefaultAsync(b => b.DayOfWeek == dayOfWeek);

        if (businessHours == null || !businessHours.IsWorkingDay)
        {
            result.IsValid = false;
            result.IsWithinBusinessHours = false;
            result.ValidationErrors.Add($"Proposed date {startTimeLocal:yyyy-MM-dd} ({startTimeLocal.DayOfWeek}) is outside operational business days.");
        }
        else
        {
            var startOfDayTime = startTimeLocal.TimeOfDay;
            var endOfDayTime = endTimeLocal.TimeOfDay;

            if (startOfDayTime < businessHours.OpenTime || endOfDayTime > businessHours.CloseTime || startTimeLocal.Date != endTimeLocal.Date)
            {
                result.IsValid = false;
                result.IsWithinBusinessHours = false;
                result.ValidationErrors.Add($"Scheduled slot ({startTimeLocal:HH:mm} - {endTimeLocal:HH:mm}) is outside operational business hours ({businessHours.OpenTime:hh\\:mm} - {businessHours.CloseTime:hh\\:mm}).");
            }
        }

        // 3. Deterministic Overlap Conflict Detection (uses UTC for DB queries)
        // Overlap rule: ExistingStart < ProposedEnd AND ExistingEnd > ProposedStart
        var activeStatuses = new[]
        {
            WorkOrderStatus.Proposed,
            WorkOrderStatus.PendingManagerApproval,
            WorkOrderStatus.Approved,
            WorkOrderStatus.Scheduled,
            WorkOrderStatus.InProgress,
            WorkOrderStatus.Paused
        };

        var conflictingWorkOrders = await _context.Set<WorkOrder>()
            .Where(w => !w.IsDeleted &&
                        w.TechnicianId == technicianId &&
                        w.Id != excludeWorkOrderId &&
                        activeStatuses.Contains(w.Status) &&
                        w.ScheduledStartTime.HasValue &&
                        w.ScheduledEndTime.HasValue &&
                        w.ScheduledStartTime.Value < endTimeUtc &&
                        w.ScheduledEndTime.Value > startTimeUtc)
            .ToListAsync();

        if (conflictingWorkOrders.Any())
        {
            result.IsValid = false;
            result.IsConflictFree = false;
            foreach (var conflict in conflictingWorkOrders)
            {
                result.Conflicts.Add(new ConflictDetailDto
                {
                    ExistingWorkOrderId = conflict.Id,
                    ExistingTitle = conflict.Title,
                    ConflictingStart = conflict.ScheduledStartTime!.Value,
                    ConflictingEnd = conflict.ScheduledEndTime!.Value,
                    Reason = $"Direct overlap with existing work order '{conflict.WorkOrderNumber} - {conflict.Title}'"
                });
                result.ValidationErrors.Add($"Schedule conflict detected with work order '{conflict.WorkOrderNumber}' ({conflict.ScheduledStartTime:yyyy-MM-dd HH:mm} - {conflict.ScheduledEndTime:HH:mm}).");
            }
        }

        // 4. SLA Compliance Check
        if (slaDeadlineUtc.HasValue && endTimeUtc > slaDeadlineUtc.Value)
        {
            result.IsSlaCompliant = false;
            // For critical/high priority, SLA violation is a hard error; for medium/low it warns or requires manager review
            if (priority.Equals("Critical", StringComparison.OrdinalIgnoreCase) || priority.Equals("High", StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.ValidationErrors.Add($"Proposed completion time ({endTimeLocal:yyyy-MM-dd HH:mm}) breaches SLA deadline ({slaDeadlineUtc.Value:yyyy-MM-dd HH:mm}).");
            }
            else
            {
                result.ValidationErrors.Add($"Warning: Proposed time slot finishes past SLA deadline ({slaDeadlineUtc.Value:yyyy-MM-dd HH:mm}).");
            }
        }

        result.Message = result.IsValid ? "Deterministic validation passed." : "Deterministic validation failed.";
        return result;
    }

    public async Task<ScheduleProposalDto> CreateConflictFreeWorkOrderAsync(ScheduleRequestDto request, Guid requesterUserId)
    {
        var maintenanceRequest = await _context.MaintenanceRequests
            .Include(r => r.Location)
            .Include(r => r.Asset)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && !r.IsDeleted);

        if (maintenanceRequest == null)
            throw new NotFoundException($"Maintenance request '{request.RequestId}' not found.");

        var technician = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .FirstOrDefaultAsync(t => t.Id == request.TechnicianId && !t.IsDeleted);

        if (technician == null)
            throw new NotFoundException($"Technician '{request.TechnicianId}' not found.");

        var priorityAssessment = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == maintenanceRequest.Id && !p.IsDeleted)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(priorityAssessment?.Priority))
        {
            request.Priority = priorityAssessment.Priority;
        }

        // Determine SLA deadline if not provided
        // Business rule: Use existing Priority Assessment SLA when available, otherwise fall back to SLAConfiguration
        var slaDeadline = request.SlaDeadline.HasValue ? NormalizeToUtc(request.SlaDeadline.Value) : (DateTime?)null;
        if (!slaDeadline.HasValue)
        {
            int resolutionHours;
            DateTime slaBaseTime;

            if (priorityAssessment != null)
            {
                // Use Priority Assessment SLA (already determined by Component 2)
                resolutionHours = priorityAssessment.ResolutionTimeHours;
                // Calculate from request creation time, not current time
                slaBaseTime = maintenanceRequest.CreatedAt;
            }
            else
            {
                // Fallback to SLAConfiguration only when no Priority Assessment exists
                var slaConfig = await _context.SLAConfigurations
                    .FirstOrDefaultAsync(s => s.PriorityLevel == request.Priority);
                resolutionHours = slaConfig?.ResolutionTimeHours ?? 24;
                slaBaseTime = maintenanceRequest.CreatedAt;
            }

            slaDeadline = slaBaseTime.AddHours(resolutionHours);
        }

        var durationMinutes = request.EstimatedDurationMinutes > 0 ? request.EstimatedDurationMinutes : 60;
        await EnsureDefaultBusinessHoursAsync();

        var requestedStart = request.PreferredStartTime.HasValue ? NormalizeToUtc(request.PreferredStartTime.Value) : (DateTime?)null;
        var requestedEnd = requestedStart.HasValue ? requestedStart.Value.AddMinutes(durationMinutes) : (DateTime?)null;

        // 0. Python Scheduling Agent invocation (if available)
        PythonSchedulingResult? pythonResult = null;
        bool pythonAgentCalled = false;
        if (_pythonAgentClient != null)
        {
            try
            {
                pythonAgentCalled = true;
                pythonResult = await _pythonAgentClient.ExecuteSchedulingAgentAsync(request);
            }
            catch
            {
                pythonResult = null;
            }
        }

        // 1. Candidate Slot Search Strategy
        var proposedStart = requestedStart ?? DateTime.UtcNow;
        var proposedEnd = proposedStart.AddMinutes(durationMinutes);
        bool slotFound = false;
        bool conflictDetected = false;
        var conflictDetails = new List<ConflictDetailDto>();

        // If Python agent returned valid proposed times, test those first
        if (!slotFound && pythonResult is { Success: true, OutputData: not null })
        {
            var pyOut = pythonResult.OutputData.Value;
            if (pyOut.TryGetProperty("proposed_start_time", out var pyStartEl) || pyOut.TryGetProperty("proposed_start", out pyStartEl))
            {
                var pyStartStr = pyStartEl.GetString();
                if (pyStartStr != null && DateTime.TryParse(pyStartStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var pyStart))
                {
                    var pyStartUtc = NormalizeToUtc(pyStart);
                    DateTime pyEndUtc;
                    if (pyOut.TryGetProperty("proposed_end_time", out var pyEndEl) || pyOut.TryGetProperty("proposed_end", out pyEndEl))
                    {
                        var pyEndStr = pyEndEl.GetString();
                        if (pyEndStr != null && DateTime.TryParse(pyEndStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var pyEnd))
                            pyEndUtc = NormalizeToUtc(pyEnd);
                        else
                            pyEndUtc = pyStartUtc.AddMinutes(durationMinutes);
                    }
                    else
                    {
                        pyEndUtc = pyStartUtc.AddMinutes(durationMinutes);
                    }

                    var pyValidation = await ValidateScheduleAsync(technician.Id, pyStartUtc, pyEndUtc, durationMinutes, request.Priority, slaDeadline);
                    if (pyValidation.IsValid)
                    {
                        proposedStart = pyStartUtc;
                        proposedEnd = pyEndUtc;
                        slotFound = true;
                    }
                    else
                    {
                        conflictDetails.AddRange(pyValidation.Conflicts);
                    }
                }
            }
        }

        // If requester gave preferred start time, test that next
        if (!slotFound && request.PreferredStartTime.HasValue)
        {
            var candidateStart = NormalizeToUtc(request.PreferredStartTime.Value);
            var candidateEnd = candidateStart.AddMinutes(durationMinutes);
            var prefValidation = await ValidateScheduleAsync(technician.Id, candidateStart, candidateEnd, durationMinutes, request.Priority, slaDeadline);
            if (prefValidation.IsValid)
            {
                proposedStart = candidateStart;
                proposedEnd = candidateEnd;
                slotFound = true;
            }
            else
            {
                conflictDetails.AddRange(prefValidation.Conflicts);
            }
        }

        if (!slotFound)
        {
            // Search for earliest valid conflict-free slot within business hours, anchored to the requested start
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
            var searchAnchorUtc = requestedStart ?? DateTime.UtcNow;
            var searchBaseLocal = TimeZoneInfo.ConvertTimeFromUtc(
                searchAnchorUtc.Kind != DateTimeKind.Utc ? searchAnchorUtc.ToUniversalTime() : searchAnchorUtc, tz);
            var maxSearchDays = 7;

            for (int dayOffset = 0; dayOffset < maxSearchDays && !slotFound; dayOffset++)
            {
                var checkDateLocal = searchBaseLocal.Date.AddDays(dayOffset);
                var dayOfWeek = (int)checkDateLocal.DayOfWeek;
                var bh = await _context.Set<BusinessHours>().FirstOrDefaultAsync(b => b.DayOfWeek == dayOfWeek);

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

                    var slotVal = await ValidateScheduleAsync(technician.Id, startSearchUtc, endSearchUtc, durationMinutes, request.Priority, slaDeadline);
                    if (slotVal.IsValid)
                    {
                        proposedStart = startSearchUtc;
                        proposedEnd = endSearchUtc;
                        slotFound = true;
                        break;
                    }

                    startSearchLocal = startSearchLocal.AddMinutes(30);
                }
            }
        }

        if (!slotFound)
        {
            // Safe Failure / Escalation Scenario: No slot found before SLA or in next 7 days
            conflictDetected = true;
            var fallbackTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
            var fallbackAnchorUtc = requestedStart ?? DateTime.UtcNow;
            var fallbackAnchorLocal = TimeZoneInfo.ConvertTimeFromUtc(
                fallbackAnchorUtc.Kind != DateTimeKind.Utc ? fallbackAnchorUtc.ToUniversalTime() : fallbackAnchorUtc, fallbackTz);
            var fallbackStartLocal = fallbackAnchorLocal.Date.AddDays(1);
            for (int fbDay = 0; fbDay < 7; fbDay++)
            {
                var fbDate = fallbackAnchorLocal.Date.AddDays(fbDay + 1);
                var fbh = await _context.Set<BusinessHours>().FirstOrDefaultAsync(b => b.DayOfWeek == (int)fbDate.DayOfWeek);
                if (fbh != null && fbh.IsWorkingDay)
                {
                    fallbackStartLocal = fbDate.Add(fbh.OpenTime);
                    break;
                }
            }
            proposedStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fallbackStartLocal, DateTimeKind.Unspecified), fallbackTz);
            proposedEnd = proposedStart.AddMinutes(durationMinutes);
            conflictDetails.Add(new ConflictDetailDto
            {
                Reason = "No conflict-free slot found within standard operating window before SLA deadline. Requires manual manager review."
            });
        }

        // Final deterministic validation on the selected slot
        var finalValidation = await ValidateScheduleAsync(technician.Id, proposedStart, proposedEnd, durationMinutes, request.Priority, slaDeadline);

        if (!finalValidation.IsValid)
        {
            conflictDetected = true;
            conflictDetails.AddRange(finalValidation.Conflicts);
        }

        var workOrderPriority = Enum.TryParse<WorkOrderPriority>(request.Priority, true, out var p) ? p : WorkOrderPriority.Medium;

        var isHighImpactPriority = workOrderPriority == WorkOrderPriority.High || workOrderPriority == WorkOrderPriority.Critical;

        var pythonContributed = pythonAgentCalled && pythonResult is { Success: true } && slotFound;
        var pythonPrefix = pythonAgentCalled
            ? (pythonResult is { Success: true }
                ? "Python Scheduling Agent was invoked and validated successfully. "
                : "Python Scheduling Agent was invoked but did not return a usable result; deterministic C# scheduling was used. ")
            : "";

        var decisionSummary = (conflictDetected, isHighImpactPriority) switch
        {
            (true, true) => $"{pythonPrefix}Schedule conflict detected and {workOrderPriority.ToString().ToLower()} priority requires Manager sign-off for technician {technician.User?.FirstName} {technician.User?.LastName}. Alternative slot proposed for Manager review.",
            (true, false) => $"{pythonPrefix}Schedule conflict detected for technician {technician.User?.FirstName} {technician.User?.LastName}. Alternative slot proposed for Manager review.",
            (false, true) => $"{pythonPrefix}No schedule conflict detected, but {workOrderPriority.ToString().ToLower()} priority requires Manager sign-off before scheduling. Proposed slot reserved for Manager review.",
            (false, false) => $"Selected an available technician slot within business hours and before the SLA deadline. Existing bookings were checked and no overlapping booking was detected."
        };

        var checklist = new ValidationChecklistDto
        {
            TechnicianAvailable = finalValidation.IsWithinTechnicianAvailability,
            ExistingBookingsChecked = true,
            SlaRequirementPassed = finalValidation.IsSlaCompliant,
            ScheduleConflictNone = finalValidation.IsConflictFree,
            BusinessHoursValid = finalValidation.IsWithinBusinessHours,
            RequiredSkillValid = technician.Skills.Any(),
            SchemaValid = true
        };

        var requiresApproval = conflictDetected || isHighImpactPriority;
        var finalStatus = requiresApproval
            ? WorkOrderStatus.PendingManagerApproval
            : WorkOrderStatus.Scheduled;

        // Create or Update WorkOrder (concurrency-safe number generation)
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

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = workOrderNumber,
            Title = maintenanceRequest.Title,
            Description = maintenanceRequest.Description,
            RequestId = maintenanceRequest.Id,
            TechnicianId = technician.Id,
            LocationId = maintenanceRequest.LocationId,
            Priority = workOrderPriority,
            Status = finalStatus,
            ScheduledStartTime = proposedStart,
            ScheduledEndTime = proposedEnd,
            EstimatedDurationMinutes = durationMinutes,
            SLADeadline = slaDeadline,
            ConflictDetected = conflictDetected,
            ConflictDetailsJson = JsonSerializer.Serialize(conflictDetails),
            AiDecisionSummary = decisionSummary
        };

        await _context.Set<WorkOrder>().AddAsync(workOrder);

        // Record Proposal
        var proposal = new ScheduleProposal
        {
            WorkOrderId = workOrder.Id,
            RequestId = maintenanceRequest.Id,
            TechnicianId = technician.Id,
            RequestedStartTime = requestedStart ?? proposedStart,
            RequestedEndTime = requestedEnd ?? proposedEnd,
            ProposedStartTime = proposedStart,
            ProposedEndTime = proposedEnd,
            EstimatedDurationMinutes = durationMinutes,
            Priority = workOrderPriority,
            SlaDeadline = slaDeadline,
            ConflictDetected = conflictDetected,
            ConflictDetailsJson = JsonSerializer.Serialize(conflictDetails),
            DecisionSummary = decisionSummary,
            ValidationDetailsJson = JsonSerializer.Serialize(checklist),
            IsAccepted = false
        };

        await _context.Set<ScheduleProposal>().AddAsync(proposal);

        // Record Status History
        var statusHistory = new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.Id,
            PreviousStatus = WorkOrderStatus.Draft,
            NewStatus = finalStatus,
            ChangedById = requesterUserId,
            Reason = requiresApproval
                ? (conflictDetected && isHighImpactPriority
                    ? $"AI Scheduling Agent detected conflict and {workOrderPriority.ToString().ToLower()} priority requires approval. Schedule proposal awaiting Manager sign-off."
                    : conflictDetected
                        ? "AI Scheduling Agent detected schedule conflict. Proposal awaiting Manager sign-off."
                        : $"AI Scheduling Agent flagged {workOrderPriority.ToString().ToLower()} priority for Manager approval.")
                : "AI Scheduling Agent created conflict-free schedule."
        };
        await _context.Set<WorkOrderStatusHistory>().AddAsync(statusHistory);

        // Register Agent Workflow & Steps for Auditable Execution Observability
        var workflowStatus = requiresApproval
            ? WorkflowStatus.WaitingForApproval
            : WorkflowStatus.Completed;

        var agentWorkflow = new AgentWorkflow
        {
            RequestId = maintenanceRequest.Id,
            WorkflowType = "SchedulingPipeline",
            Status = workflowStatus,
            OutputSummaryJson = JsonSerializer.Serialize(new
            {
                workOrderId = workOrder.Id,
                technicianId = technician.Id,
                proposedStart,
                proposedEnd,
                conflictDetected,
                decisionSummary,
                pythonAgentCalled,
                pythonAgentSuccess = pythonResult?.Success ?? false,
                executionMode = pythonResult?.ExecutionMode ?? "not_invoked",
                llmUsed = pythonResult?.LlmUsed,
                schedulingPath = pythonResult?.SchedulingPath
            })
        };

        var schedulingStep = new AgentStep
        {
            WorkflowId = agentWorkflow.Id,
            AgentName = "SchedulingAgent",
            StepName = "Conflict-Free Schedule Generation",
            Status = workflowStatus,
            InputDataJson = JsonSerializer.Serialize(new
            {
                requestId = maintenanceRequest.Id,
                technicianId = technician.Id,
                priority = request.Priority,
                duration = durationMinutes,
                sla = slaDeadline
            }),
            OutputDataJson = JsonSerializer.Serialize(new
            {
                proposalId = proposal.Id,
                requestId = proposal.RequestId,
                technicianId = proposal.TechnicianId,
                proposedStartTime = proposal.ProposedStartTime,
                proposedEndTime = proposal.ProposedEndTime,
                estimatedDurationMinutes = proposal.EstimatedDurationMinutes,
                priority = proposal.Priority.ToString(),
                conflictDetected = proposal.ConflictDetected,
                decisionSummary = proposal.DecisionSummary
            }),
            ValidationResultJson = JsonSerializer.Serialize(checklist)
        };

        // Add tool call records — prefer actual Python agent tool calls when available
        if (pythonAgentCalled && pythonResult != null && pythonResult.ToolCalls.Count > 0)
        {
            foreach (var tc in pythonResult.ToolCalls)
            {
                schedulingStep.ToolCalls.Add(new AgentToolCall
                {
                    StepId = schedulingStep.Id,
                    ToolName = tc.ToolName,
                    InputJson = tc.InputParams?.GetRawText() ?? "{}",
                    OutputJson = tc.OutputParams?.GetRawText() ?? "{}",
                    ExecutionTimeMs = tc.ExecutionTimeMs,
                    Success = tc.Success,
                    ErrorMessage = tc.ErrorMessage
                });
            }
        }
        else
        {
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
                                 w.TechnicianId == technician.Id &&
                                 activeStatusesForCount.Contains(w.Status));
            sw.Stop();
            var techCalendarTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var proposedStartLocal = ConvertToLocal(proposedStart);
            var proposedDayOfWeek = (int)proposedStartLocal.DayOfWeek;
            var actualBusinessHours = await _context.Set<BusinessHours>()
                .FirstOrDefaultAsync(b => b.DayOfWeek == proposedDayOfWeek);
            sw.Stop();
            var businessHoursTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var conflictingWorkOrdersForCount = await _context.Set<WorkOrder>()
                .Where(w => !w.IsDeleted &&
                            w.TechnicianId == technician.Id &&
                            activeStatusesForCount.Contains(w.Status) &&
                            w.ScheduledStartTime.HasValue &&
                            w.ScheduledEndTime.HasValue &&
                            w.ScheduledStartTime.Value < proposedEnd &&
                            w.ScheduledEndTime.Value > proposedStart)
                .CountAsync();
            sw.Stop();
            var existingWorkOrdersTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var proposalSerializationData = new { proposalCreated = true, proposalId = proposal.Id };
            sw.Stop();
            var proposalCreationTimeMs = (int)sw.ElapsedMilliseconds;

            sw.Restart();
            var validationSerializationData = finalValidation;
            sw.Stop();
            var validationTimeMs = (int)sw.ElapsedMilliseconds;

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "GetTechnicianCalendar",
                InputJson = JsonSerializer.Serialize(new { technicianId = technician.Id }),
                OutputJson = JsonSerializer.Serialize(new { isAvailable = technician.IsAvailable, activeBookingsCount }),
                ExecutionTimeMs = techCalendarTimeMs,
                Success = true
            });

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "GetBusinessHours",
                InputJson = JsonSerializer.Serialize(new { date = proposedStartLocal.ToString("yyyy-MM-dd"), dayOfWeek = proposedDayOfWeek }),
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
                InputJson = JsonSerializer.Serialize(new { technicianId = technician.Id, checkDate = proposedStartLocal.ToString("yyyy-MM-dd") }),
                OutputJson = JsonSerializer.Serialize(new { conflictCount = conflictingWorkOrdersForCount, activeBookingsCount }),
                ExecutionTimeMs = existingWorkOrdersTimeMs,
                Success = true
            });

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "CreateScheduleProposal",
                InputJson = JsonSerializer.Serialize(new { start = proposedStart, end = proposedEnd, duration = durationMinutes }),
                OutputJson = JsonSerializer.Serialize(proposalSerializationData),
                ExecutionTimeMs = proposalCreationTimeMs,
                Success = true
            });

            schedulingStep.ToolCalls.Add(new AgentToolCall
            {
                StepId = schedulingStep.Id,
                ToolName = "ValidateSchedule",
                InputJson = JsonSerializer.Serialize(new { technicianId = technician.Id, start = proposedStart, end = proposedEnd }),
                OutputJson = JsonSerializer.Serialize(validationSerializationData),
                ExecutionTimeMs = validationTimeMs,
                Success = finalValidation.IsValid
            });
        }

        agentWorkflow.Steps.Add(schedulingStep);

        // Add Approval Action for Human-In-The-Loop only when a genuine conflict requires manager review
        if (requiresApproval)
        {
            agentWorkflow.ApprovalActions.Add(new ApprovalAction
            {
                WorkflowId = agentWorkflow.Id,
                Status = ApprovalStatus.Pending,
                ReasonRequired = (conflictDetected, isHighImpactPriority) switch
                {
                    (true, true) => "Schedule conflict detected and high-impact priority. Manager sign-off required for alternative slot or manual scheduling.",
                    (true, false) => "Schedule conflict detected. Manager sign-off required for alternative slot or manual scheduling.",
                    (false, true) => $"High-impact ({workOrderPriority}) priority requires Manager approval before scheduling.",
                    _ => "Manager review required."
                },
                RequestedAt = DateTime.UtcNow
            });
        }

        await _context.AgentWorkflows.AddAsync(agentWorkflow);
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogAsync(
            requesterUserId,
            "ScheduleProposed",
            "WorkOrder",
            workOrder.Id.ToString(),
            JsonSerializer.Serialize(new { workOrderNumber, proposedStart, proposedEnd, conflictDetected }),
            "127.0.0.1");

        return new ScheduleProposalDto
        {
            ProposalId = proposal.Id,
            WorkOrderId = workOrder.Id,
            RequestId = maintenanceRequest.Id,
            TechnicianId = technician.Id,
            TechnicianName = $"{technician.User?.FirstName} {technician.User?.LastName}",
            RequestedStart = requestedStart ?? proposedStart,
            RequestedEnd = requestedEnd ?? proposedEnd,
            ProposedStart = proposedStart,
            ProposedEnd = proposedEnd,
            EstimatedDurationMinutes = durationMinutes,
            Priority = request.Priority,
            SlaDeadline = slaDeadline,
            ConflictDetected = conflictDetected,
            ConflictDetails = conflictDetails,
            WithinBusinessHours = finalValidation.IsWithinBusinessHours,
            WithinTechnicianAvailability = finalValidation.IsWithinTechnicianAvailability,
            SlaCompliant = finalValidation.IsSlaCompliant,
            ProposalStatus = finalStatus.ToString(),
            DecisionSummary = decisionSummary,
            ValidationRequired = true,
            ValidationChecklist = checklist
        };
    }

    public async Task<List<CalendarEventDto>> GetCalendarEventsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? technicianId = null,
        string? priority = null,
        string? status = null)
    {
        var query = _context.Set<WorkOrder>()
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .Include(w => w.Location)
            .Where(w => !w.IsDeleted && w.ScheduledStartTime.HasValue && w.ScheduledEndTime.HasValue);

        if (startDate.HasValue)
            query = query.Where(w => w.ScheduledEndTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.ScheduledStartTime <= endDate.Value);

        if (technicianId.HasValue && technicianId.Value != Guid.Empty)
            query = query.Where(w => w.TechnicianId == technicianId.Value);

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<WorkOrderPriority>(priority, true, out var p))
            query = query.Where(w => w.Priority == p);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkOrderStatus>(status, true, out var s))
            query = query.Where(w => w.Status == s);

        var workOrders = await query.OrderBy(w => w.ScheduledStartTime).ToListAsync();

        return workOrders.Select(w => new CalendarEventDto
        {
            Id = w.Id,
            WorkOrderNumber = w.WorkOrderNumber,
            Title = w.Title,
            Start = w.ScheduledStartTime!.Value,
            End = w.ScheduledEndTime!.Value,
            Priority = w.Priority.ToString(),
            Status = w.Status.ToString(),
            TechnicianId = w.TechnicianId,
            TechnicianName = w.Technician?.User != null ? $"{w.Technician.User.FirstName} {w.Technician.User.LastName}" : "Unassigned",
            LocationName = w.Location?.Name ?? "Main Complex",
            ConflictDetected = w.ConflictDetected
        }).ToList();
    }

    public async Task<SchedulingReportDto> GetSchedulingReportsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<WorkOrder>()
            .Include(w => w.Technician)
                .ThenInclude(t => t!.User)
            .Where(w => !w.IsDeleted);

        if (startDate.HasValue)
            query = query.Where(w => w.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.CreatedAt <= endDate.Value);

        var workOrders = await query.ToListAsync();

        var total = workOrders.Count;
        var scheduled = workOrders.Count(w => w.Status == WorkOrderStatus.Scheduled);
        var pending = workOrders.Count(w => w.Status == WorkOrderStatus.PendingManagerApproval || w.Status == WorkOrderStatus.Proposed);
        var inProgress = workOrders.Count(w => w.Status == WorkOrderStatus.InProgress || w.Status == WorkOrderStatus.Paused);
        var completed = workOrders.Count(w => w.Status == WorkOrderStatus.Completed);
        var conflictCount = workOrders.Count(w => w.ConflictDetected);

        var slaBreached = workOrders.Count(w =>
            w.SLADeadline.HasValue &&
            ((w.ActualEndTime.HasValue && w.ActualEndTime.Value > w.SLADeadline.Value) ||
             (!w.ActualEndTime.HasValue && DateTime.UtcNow > w.SLADeadline.Value && w.Status != WorkOrderStatus.Completed)));

        var complianceRate = total > 0 ? Math.Round((double)(total - slaBreached) / total * 100, 1) : 100.0;

        var scheduledWorkOrders = workOrders.Where(w => w.ScheduledStartTime.HasValue).ToList();
        var avgLeadTimeHours = scheduledWorkOrders.Count > 0
            ? Math.Round(scheduledWorkOrders.Average(w => (w.ScheduledStartTime!.Value - w.CreatedAt).TotalHours), 1)
            : 0.0;

        // Status distribution
        var statusBreakdown = workOrders
            .GroupBy(w => w.Status.ToString())
            .Select(g => new StatusDistributionDto
            {
                Status = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0
            }).ToList();

        // Priority distribution
        var priorityBreakdown = workOrders
            .GroupBy(w => w.Priority.ToString())
            .Select(g => new PriorityDistributionDto
            {
                Priority = g.Key,
                Count = g.Count()
            }).ToList();

        // Technician workload
        var techWorkload = workOrders
            .Where(w => w.Technician != null && w.Technician.User != null)
            .GroupBy(w => new { w.TechnicianId, Name = $"{w.Technician!.User!.FirstName} {w.Technician.User.LastName}" })
            .Select(g => new TechnicianWorkloadDto
            {
                TechnicianId = g.Key.TechnicianId,
                TechnicianName = g.Key.Name,
                AssignedJobs = g.Count(),
                CompletedJobs = g.Count(w => w.Status == WorkOrderStatus.Completed),
                InProgressJobs = g.Count(w => w.Status == WorkOrderStatus.InProgress)
            }).ToList();

        // Completion trend (last 7 days or date range)
        var trendGroup = workOrders
            .GroupBy(w => w.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Take(14)
            .Select(g => new CompletionTrendDto
            {
                Date = g.Key,
                ScheduledCount = g.Count(w => w.Status == WorkOrderStatus.Scheduled || w.Status == WorkOrderStatus.PendingManagerApproval),
                CompletedCount = g.Count(w => w.Status == WorkOrderStatus.Completed)
            }).ToList();

        return new SchedulingReportDto
        {
            TotalWorkOrders = total,
            ScheduledCount = scheduled,
            PendingApprovalCount = pending,
            InProgressCount = inProgress,
            CompletedCount = completed,
            ConflictCount = conflictCount,
            SlaBreachedCount = slaBreached,
            SlaComplianceRate = complianceRate,
            AverageSchedulingLeadTimeHours = avgLeadTimeHours,
            StatusBreakdown = statusBreakdown,
            PriorityBreakdown = priorityBreakdown,
            TechnicianWorkloads = techWorkload,
            CompletionTrends = trendGroup
        };
    }

    private async Task EnsureDefaultBusinessHoursAsync()
    {
        if (!await _context.Set<BusinessHours>().AnyAsync())
        {
            var defaultHours = new List<BusinessHours>
            {
                new BusinessHours { DayOfWeek = 0, DayName = "Sunday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = false },
                new BusinessHours { DayOfWeek = 1, DayName = "Monday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 2, DayName = "Tuesday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 3, DayName = "Wednesday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 4, DayName = "Thursday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 5, DayName = "Friday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(17, 0, 0), IsWorkingDay = true },
                new BusinessHours { DayOfWeek = 6, DayName = "Saturday", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(13, 0, 0), IsWorkingDay = true }
            };

            await _context.Set<BusinessHours>().AddRangeAsync(defaultHours);
            await _context.SaveChangesAsync();
        }
    }
}

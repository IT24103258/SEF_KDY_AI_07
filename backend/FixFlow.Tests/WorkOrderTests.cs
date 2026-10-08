using System.Text.Json;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using FixFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FixFlow.Tests;

public class WorkOrderTests
{
    private static readonly TimeZoneInfo FacilityTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    private FixFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"FixFlow_TestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new FixFlowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task<(FixFlowDbContext context, Technician tech, MaintenanceRequest request, User manager, User requester)> SeedBasicTestData(FixFlowDbContext context)
    {
        var roleManager = new Role { Name = "Manager", Description = "Manager role" };
        var roleTech = new Role { Name = "Technician", Description = "Tech role" };
        var roleReq = new Role { Name = "Requester", Description = "Requester role" };
        await context.Roles.AddRangeAsync(roleManager, roleTech, roleReq);

        var manager = new User { Email = "manager@fixflow.local", FirstName = "Test", LastName = "Manager", RoleId = roleManager.Id };
        var techUser = new User { Email = "tech@fixflow.local", FirstName = "Tech", LastName = "User", RoleId = roleTech.Id };
        var requester = new User { Email = "req@fixflow.local", FirstName = "Resident", LastName = "Requester", RoleId = roleReq.Id };
        await context.Users.AddRangeAsync(manager, techUser, requester);

        var loc = new Location { Name = "Tower A - Unit 305", Building = "Tower A", Floor = "3", Room = "305" };
        await context.Locations.AddAsync(loc);

        var skill = new Skill { Name = "Electrical", Category = "Electrical" };
        await context.Skills.AddAsync(skill);

        var tech = new Technician
        {
            UserId = techUser.Id,
            EmployeeId = "TECH-001",
            Specialization = "Electrical",
            IsAvailable = true,
            User = techUser,
            Skills = new List<Skill> { skill }
        };
        await context.Technicians.AddAsync(tech);

        var request = new MaintenanceRequest
        {
            RequestNumber = "REQ-2026-0001",
            Title = "Ceiling Light Short Circuit",
            Description = "Bathroom light sparked and tripped breaker.",
            Status = RequestStatus.Matched,
            LocationId = loc.Id,
            RequesterId = requester.Id,
            CreatedAt = DateTime.UtcNow
        };
        await context.MaintenanceRequests.AddAsync(request);

        var assignment = new Assignment
        {
            MaintenanceRequestId = request.Id,
            TechnicianId = tech.Id,
            MatchScore = 0.95,
            ReasoningSummary = "Test assignment for unit tests",
            Status = "Assigned",
            AssignedAt = DateTime.UtcNow
        };
        await context.Set<Assignment>().AddAsync(assignment);

        // Seed SLA configurations with generous resolution times to account for non-business day test runs
        var slas = new List<SLAConfiguration>
        {
            new SLAConfiguration { PriorityLevel = "High", ResponseTimeHours = 24, ResolutionTimeHours = 120 },
            new SLAConfiguration { PriorityLevel = "Medium", ResponseTimeHours = 48, ResolutionTimeHours = 120 },
            new SLAConfiguration { PriorityLevel = "Low", ResponseTimeHours = 72, ResolutionTimeHours = 120 }
        };
        await context.SLAConfigurations.AddRangeAsync(slas);

        await context.SaveChangesAsync();
        return (context, tech, request, manager, requester);
    }

    private DateTime GetNextWeekdayUtc()
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(1);
        while (targetDate.DayOfWeek == DayOfWeek.Saturday || targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDate = targetDate.AddDays(1);
        }
        return targetDate;
    }

    private DateTime GetNextWeekdayLocal()
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FacilityTimeZone);
        var targetDate = localNow.Date.AddDays(1);
        while (targetDate.DayOfWeek == DayOfWeek.Saturday || targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDate = targetDate.AddDays(1);
        }
        return targetDate;
    }

    private DateTime LocalToUtc(DateTime localTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), FacilityTimeZone);
    }

    [Fact]
    public async Task ValidateScheduleAsync_ShouldDetectConflict_WhenDirectOverlapExists()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();

        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var targetDate = GetNextWeekdayUtc();

        var existingBooking = new WorkOrder
        {
            WorkOrderNumber = "WO-TEST-001",
            Title = "Existing Morning Job",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = targetDate.AddHours(9),
            ScheduledEndTime = targetDate.AddHours(11),
            EstimatedDurationMinutes = 120
        };
        await ctx.Set<WorkOrder>().AddAsync(existingBooking);
        await ctx.SaveChangesAsync();

        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id,
            targetDate.AddHours(10),
            targetDate.AddHours(12),
            120,
            "High");

        Assert.False(valResult.IsValid);
        Assert.False(valResult.IsConflictFree);
        Assert.NotEmpty(valResult.Conflicts);
        Assert.Contains(valResult.Conflicts, c => c.ExistingWorkOrderId == existingBooking.Id);
    }

    [Fact]
    public async Task ValidateScheduleAsync_ShouldFail_WhenSlotOutsideBusinessHours()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();

        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var targetDate = GetNextWeekdayUtc();
        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id,
            targetDate.AddHours(22),
            targetDate.AddHours(23).AddMinutes(30),
            90,
            "Medium");

        Assert.False(valResult.IsValid);
        Assert.False(valResult.IsWithinBusinessHours);
        Assert.Contains(valResult.ValidationErrors, e => e.Contains("outside opera", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateScheduleAsync_ShouldFail_WhenTechnicianUnavailable()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        tech.IsAvailable = false;
        await ctx.SaveChangesAsync();

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();

        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var targetDate = GetNextWeekdayUtc();

        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id,
            targetDate.AddHours(10),
            targetDate.AddHours(12),
            120,
            "Medium");

        Assert.False(valResult.IsValid);
        Assert.False(valResult.IsWithinTechnicianAvailability);
    }

    [Fact]
    public async Task StateTransition_ShouldRejectInvalidStatusJumps()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var draftWo = new WorkOrder
        {
            WorkOrderNumber = "WO-DRAFT-001",
            Title = "Draft job",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Draft
        };
        await ctx.Set<WorkOrder>().AddAsync(draftWo);
        await ctx.SaveChangesAsync();

        var updateDto = new WorkOrderStatusUpdateDto { Status = "Completed" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            woService.UpdateStatusAsync(draftWo.Id, updateDto, manager.Id, "Manager"));
    }

    [Fact]
    public async Task ApproveWorkOrderAsync_ShouldFinalizeSchedule_AndRecordApproval()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDate = GetNextWeekdayUtc();

        // Use UTC times that map to within Sri Lankan business hours (08:00-17:00 LK = 02:30-11:30 UTC)
        // 05:00 UTC = 10:30 LK, 07:00 UTC = 12:30 LK
        var pendingWo = new WorkOrder
        {
            WorkOrderNumber = "WO-PENDING-001",
            Title = "Pending Manager Sign-off",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.PendingManagerApproval,
            ScheduledStartTime = targetDate.AddHours(5),
            ScheduledEndTime = targetDate.AddHours(7),
            EstimatedDurationMinutes = 120
        };
        await ctx.Set<WorkOrder>().AddAsync(pendingWo);
        await ctx.SaveChangesAsync();

        var decision = new WorkOrderApprovalDecisionDto
        {
            Approved = true,
            Comments = "Schedule approved for afternoon slot."
        };

        var approvedDto = await woService.ApproveWorkOrderAsync(pendingWo.Id, decision, manager.Id);

        Assert.Equal("Scheduled", approvedDto.Status);
        Assert.Equal(manager.Id, approvedDto.ApprovedById);
        Assert.NotNull(approvedDto.ApprovedAt);
        Assert.Equal("Schedule approved for afternoon slot.", approvedDto.ApprovalComments);

        mockNotify.Verify(n => n.SendNotificationAsync(tech.UserId, It.IsAny<string>(), It.IsAny<string>(), "Info"), Times.Once);
    }

    [Fact]
    public async Task RejectWorkOrderAsync_ShouldMarkRejected_AndRecordComments()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var pendingWo = new WorkOrder
        {
            WorkOrderNumber = "WO-REJECT-001",
            Title = "Job To Reject",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.PendingManagerApproval
        };
        await ctx.Set<WorkOrder>().AddAsync(pendingWo);
        await ctx.SaveChangesAsync();

        var decision = new WorkOrderApprovalDecisionDto
        {
            Approved = false,
            Comments = "Technician lacks high-voltage certification."
        };

        var rejectedDto = await woService.RejectWorkOrderAsync(pendingWo.Id, decision, manager.Id);

        Assert.Equal("Rejected", rejectedDto.Status);
        Assert.Equal("Technician lacks high-voltage certification.", rejectedDto.ApprovalComments);
    }

    [Fact]
    public async Task CompleteWorkOrderAsync_ShouldRecordSignature_AndTransitionCompleted()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var inProgressWo = new WorkOrder
        {
            WorkOrderNumber = "WO-INPROG-001",
            Title = "In Progress Maintenance",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Technician = tech,
            Status = WorkOrderStatus.InProgress,
            ScheduledStartTime = DateTime.UtcNow.AddHours(-1),
            ScheduledEndTime = DateTime.UtcNow.AddHours(1),
            ActualStartTime = DateTime.UtcNow.AddMinutes(-50)
        };
        await ctx.Set<WorkOrder>().AddAsync(inProgressWo);
        await ctx.SaveChangesAsync();

        var completeDto = new CompleteWorkOrderDto
        {
            SignerName = "Jane Doe",
            SignatureDataUrl = "data:image/svg+xml;utf8,<svg>sig</svg>",
            CompletionNotes = "Replaced faulty switch and checked grounding."
        };

        var completedResult = await woService.CompleteWorkOrderAsync(inProgressWo.Id, completeDto, tech.UserId);

        Assert.Equal("Completed", completedResult.Status);
        Assert.NotNull(completedResult.ActualEndTime);
        Assert.NotEmpty(completedResult.Evidence);
        Assert.Equal("Jane Doe", completedResult.Evidence.First().SignerName);
    }

    [Fact]
    public async Task ServerSideAuthorization_TechnicianCannotAccessOtherTechnicianJobs()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = new WorkOrder
        {
            WorkOrderNumber = "WO-OTHER-001",
            Title = "Other Tech Job",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Technician = tech,
            Status = WorkOrderStatus.Scheduled
        };
        await ctx.Set<WorkOrder>().AddAsync(wo);
        await ctx.SaveChangesAsync();

        var anotherTechUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            woService.GetWorkOrderByIdAsync(wo.Id, anotherTechUserId, "Technician"));
    }

    [Fact]
    public async Task PaginationAndFiltering_ShouldFilterByStatusAndPriority()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var woCritical = new WorkOrder
        {
            WorkOrderNumber = "WO-CRIT-001",
            Title = "Critical HVAC issue",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Priority = WorkOrderPriority.Critical,
            Status = WorkOrderStatus.Scheduled
        };

        var woLow = new WorkOrder
        {
            WorkOrderNumber = "WO-LOW-001",
            Title = "Low priority paint touchup",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Priority = WorkOrderPriority.Low,
            Status = WorkOrderStatus.Draft
        };

        await ctx.Set<WorkOrder>().AddRangeAsync(woCritical, woLow);
        await ctx.SaveChangesAsync();

        var pagedResult = await woService.GetWorkOrdersAsync(
            page: 1,
            pageSize: 10,
            priority: "Critical",
            status: "Scheduled",
            currentUserRole: "Manager");

        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal("WO-CRIT-001", pagedResult.Items.First().WorkOrderNumber);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_WithConflict_ShouldSetPendingManagerApproval_AndCreateProposal()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDate = GetNextWeekdayUtc();

        // Create an existing work order that will cause a conflict
        var existingBooking = new WorkOrder
        {
            WorkOrderNumber = "WO-EXISTING-001",
            Title = "Existing Booking",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = targetDate.AddHours(10),
            ScheduledEndTime = targetDate.AddHours(12),
            EstimatedDurationMinutes = 120
        };
        await ctx.Set<WorkOrder>().AddAsync(existingBooking);

        // Seed business hours for the target day
        var dayOfWeek = (int)targetDate.DayOfWeek;
        var businessHours = new BusinessHours
        {
            DayOfWeek = dayOfWeek,
            OpenTime = new TimeSpan(8, 0, 0),
            CloseTime = new TimeSpan(17, 0, 0),
            IsWorkingDay = true
        };
        await ctx.Set<BusinessHours>().AddAsync(businessHours);
        await ctx.SaveChangesAsync();

        // Create a new work order with a conflicting schedule (overlaps with existing booking)
        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Conflicting Work Order",
            Description = "This should conflict with existing booking",
            Priority = "High",
            ScheduledStartTime = targetDate.AddHours(11), // Overlaps with 10:00-12:00
            ScheduledEndTime = targetDate.AddHours(13),
            EstimatedDurationMinutes = 120,
            SLADeadline = targetDate.AddDays(5)
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        // Verify the work order is saved with correct status
        Assert.NotNull(result);
        Assert.True(result.ConflictDetected);
        Assert.Equal("PendingManagerApproval", result.Status);
        Assert.NotNull(result.ConflictDetailsJson);
        Assert.NotNull(result.AiDecisionSummary);

        // Verify the work order has scheduled times (either original or alternative)
        Assert.True(result.ScheduledStartTime.HasValue);
        Assert.True(result.ScheduledEndTime.HasValue);

        // Verify ScheduleProposal was created
        var proposal = await ctx.Set<ScheduleProposal>()
            .FirstOrDefaultAsync(p => p.WorkOrderId == result.Id);
        Assert.NotNull(proposal);
        Assert.True(proposal.ConflictDetected);
        Assert.Equal(result.ScheduledStartTime, proposal.ProposedStartTime);
        Assert.Equal(result.ScheduledEndTime, proposal.ProposedEndTime);

        // Verify AgentWorkflow was created
        var workflow = await ctx.AgentWorkflows
            .FirstOrDefaultAsync(w => w.RequestId == request.Id && w.WorkflowType == "SchedulingPipeline");
        Assert.NotNull(workflow);
        Assert.Equal("WaitingForApproval", workflow.Status.ToString());
        Assert.NotEmpty(workflow.Steps);
        Assert.NotEmpty(workflow.ApprovalActions);
        Assert.Equal("Pending", workflow.ApprovalActions.First().Status.ToString());

        // Verify GetPendingApprovalsAsync returns this work order
        var pendingApprovals = await woService.GetPendingApprovalsAsync();
        Assert.Contains(pendingApprovals, wo => wo.Id == result.Id);
        var pendingWo = pendingApprovals.First(wo => wo.Id == result.Id);
        Assert.Equal("PendingManagerApproval", pendingWo.Status);
        Assert.True(pendingWo.ConflictDetected);
        Assert.NotNull(pendingWo.AiDecisionSummary);
        Assert.NotNull(pendingWo.ConflictDetailsJson);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_WithoutConflict_ShouldSetScheduled_AndNotCreateApproval()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();

        // Use Sri Lankan local times within business hours (08:00-17:00)
        // These have Kind=Unspecified, matching what the frontend sends after JSON deserialization
        var startTimeLocal = targetDateLocal.AddHours(10); // 10:00 LK = 04:30 UTC
        var endTimeLocal = targetDateLocal.AddHours(12);   // 12:00 LK = 06:30 UTC
        var startTimeUtc = LocalToUtc(startTimeLocal);
        var endTimeUtc = LocalToUtc(endTimeLocal);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Non-Conflicting Work Order",
            Description = "This should not conflict with any existing booking",
            Priority = "Medium",
            ScheduledStartTime = startTimeLocal,
            ScheduledEndTime = endTimeLocal,
            EstimatedDurationMinutes = 120,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.NotNull(result);
        Assert.False(result.ConflictDetected);
        Assert.Equal("Scheduled", result.Status);
        Assert.Equal(startTimeUtc, result.ScheduledStartTime);
        Assert.Equal(endTimeUtc, result.ScheduledEndTime);

        // Verify NO ScheduleProposal was created (no conflict means no proposal needed)
        var proposal = await ctx.Set<ScheduleProposal>()
            .FirstOrDefaultAsync(p => p.WorkOrderId == result.Id);
        Assert.Null(proposal);

        // Verify NO AgentWorkflow was created (no conflict means no approval workflow)
        var workflow = await ctx.AgentWorkflows
            .FirstOrDefaultAsync(w => w.RequestId == request.Id);
        Assert.Null(workflow);

        // Verify GetPendingApprovalsAsync does NOT return this work order
        var pendingApprovals = await woService.GetPendingApprovalsAsync();
        Assert.DoesNotContain(pendingApprovals, wo => wo.Id == result.Id);
    }
    [Fact]
    public async Task WorkOrderNumber_ShouldBeUnique_AcrossMultipleCreations()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var numbers = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var newRequest = new MaintenanceRequest
            {
                RequestNumber = $"REQ-CONCURRENT-{i}",
                Title = $"Concurrent Test {i}",
                Description = "Testing unique WO numbers",
                Status = RequestStatus.Matched,
                LocationId = request.LocationId,
                RequesterId = requester.Id,
                CreatedAt = DateTime.UtcNow
            };
            await ctx.MaintenanceRequests.AddAsync(newRequest);
            await ctx.SaveChangesAsync();

            var validSlot = GetNextWeekdayUtc().AddHours(10 + i * 3);
            newRequest.CreatedAt = validSlot.AddHours(-2);
            await ctx.SaveChangesAsync();

            var reqDto = new ScheduleRequestDto
            {
                RequestId = newRequest.Id,
                TechnicianId = tech.Id,
                Priority = "Low",
                EstimatedDurationMinutes = 60,
                PreferredStartTime = validSlot
            };

            var proposal = await schedService.CreateConflictFreeWorkOrderAsync(reqDto, requester.Id);
            var wo = await ctx.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == proposal.WorkOrderId);
            numbers.Add(wo?.WorkOrderNumber ?? "");
        }

        var distinctNumbers = numbers.Distinct().ToList();
        Assert.Equal(5, distinctNumbers.Count);
    }

    // ============================================================
    // REPORTS — AverageSchedulingLeadTimeHours from actual data
    // ============================================================

    [Fact]
    public async Task GetSchedulingReportsAsync_ShouldCalculateLeadTimeFromActualData()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var now = DateTime.UtcNow;

        // Create work orders with known lead times
        await ctx.Set<WorkOrder>().AddAsync(new WorkOrder
        {
            WorkOrderNumber = "WO-LEAD-001",
            Title = "Lead Time Test 1",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            CreatedAt = now.AddHours(-48),
            ScheduledStartTime = now.AddHours(-24),
            ScheduledEndTime = now.AddHours(-22)
        });

        await ctx.Set<WorkOrder>().AddAsync(new WorkOrder
        {
            WorkOrderNumber = "WO-LEAD-002",
            Title = "Lead Time Test 2",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            CreatedAt = now.AddHours(-72),
            ScheduledStartTime = now.AddHours(-24),
            ScheduledEndTime = now.AddHours(-22)
        });

        await ctx.SaveChangesAsync();

        var report = await schedService.GetSchedulingReportsAsync();

        // Lead times: 24h and 48h → average = 36h
        Assert.Equal(36.0, report.AverageSchedulingLeadTimeHours);
    }

    [Fact]
    public async Task GetSchedulingReportsAsync_ShouldReturnZeroLeadTime_WhenNoScheduledWorkOrders()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var report = await schedService.GetSchedulingReportsAsync();

        Assert.Equal(0.0, report.AverageSchedulingLeadTimeHours);
    }
    // ============================================================
    // PYTHON SCHEDULING AGENT INTEGRATION TESTS
    // ============================================================

    private static JsonElement? MakeJsonElement(object obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();
        return root;
    }

    private static Mock<IPythonSchedulingAgentClient> CreateMockPythonClient(PythonSchedulingResult? result = null)
    {
        var mock = new Mock<IPythonSchedulingAgentClient>();
        mock.Setup(c => c.ExecuteSchedulingAgentAsync(
                It.IsAny<SchedulingAgentInvocation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    private WorkOrderService CreateWorkOrderServiceWithPython(
        FixFlowDbContext ctx,
        Mock<IPythonSchedulingAgentClient>? mockPython,
        out SchedulingService schedService)
    {
        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        return new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object,
            mockPython?.Object, Mock.Of<ILogger<WorkOrderService>>());
    }
    [Fact]
    public async Task PythonAgent_IsInvoked_WhenClientProvidedAndTimesSpecified()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockPython = CreateMockPythonClient(new PythonSchedulingResult
        {
            Success = false,
            Status = "DETERMINISTIC",
            OutputData = null,
            ToolCalls = new(),
            ExecutionMode = "deterministic_fallback"
        });

        var woService = CreateWorkOrderServiceWithPython(ctx, mockPython, out _);

        var targetDateLocal = GetNextWeekdayLocal();
        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Python Agent Invocation Test",
            Description = "Verify Python agent is called",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        await woService.CreateWorkOrderAsync(createDto, manager.Id);

        mockPython.Verify(c => c.ExecuteSchedulingAgentAsync(
            It.IsAny<SchedulingAgentInvocation>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PythonAgent_Success_UsesProposedTimesFromAgent()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var targetDateLocal = GetNextWeekdayLocal();
        var agentStartLocal = targetDateLocal.AddHours(14);
        var agentEndLocal = targetDateLocal.AddHours(15);
        var agentStartUtc = LocalToUtc(agentStartLocal);
        var agentEndUtc = LocalToUtc(agentEndLocal);

        var mockPython = CreateMockPythonClient(new PythonSchedulingResult
        {
            Success = true,
            Status = "SUCCESS",
            OutputData = MakeJsonElement(new
            {
                proposed_start_time = agentStartUtc.ToString("o"),
                proposed_end_time = agentEndUtc.ToString("o"),
                llm_used = true,
                scheduling_path = "llm_optimized"
            }),
            ToolCalls = new List<PythonAgentToolCallDto>
            {
                new() { ToolName = "GetTechnicianCalendar", Success = true, ExecutionTimeMs = 5 }
            },
            ExecutionMode = "llm_assisted",
            LlmUsed = true,
            SchedulingPath = "llm_optimized"
        });

        var woService = CreateWorkOrderServiceWithPython(ctx, mockPython, out _);

        var requestedStartLocal = targetDateLocal.AddHours(10);
        var requestedEndLocal = targetDateLocal.AddHours(11);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Python Agent Proposed Times",
            Description = "Agent should override requested times",
            Priority = "Medium",
            ScheduledStartTime = requestedStartLocal,
            ScheduledEndTime = requestedEndLocal,
            EstimatedDurationMinutes = 60,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.Equal(agentStartUtc, result.ScheduledStartTime);
        Assert.Equal(agentEndUtc, result.ScheduledEndTime);
    }

    [Fact]
    public async Task PythonAgent_Timeout_FallsBackToDeterministicScheduling()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockPython = new Mock<IPythonSchedulingAgentClient>();
        mockPython.Setup(c => c.ExecuteSchedulingAgentAsync(
                It.IsAny<SchedulingAgentInvocation>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Simulated timeout"));

        var woService = CreateWorkOrderServiceWithPython(ctx, mockPython, out _);

        var targetDateLocal = GetNextWeekdayLocal();
        var startTimeLocal = targetDateLocal.AddHours(10);
        var endTimeLocal = targetDateLocal.AddHours(11);
        var expectedStartUtc = LocalToUtc(startTimeLocal);
        var expectedEndUtc = LocalToUtc(endTimeLocal);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Python Agent Timeout Fallback",
            Description = "Should fall back to C# deterministic on timeout",
            Priority = "Medium",
            ScheduledStartTime = startTimeLocal,
            ScheduledEndTime = endTimeLocal,
            EstimatedDurationMinutes = 60,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.Equal(expectedStartUtc, result.ScheduledStartTime);
        Assert.Equal(expectedEndUtc, result.ScheduledEndTime);
        Assert.Equal("Scheduled", result.Status);
        Assert.NotNull(result.AiDecisionSummary);
    }

    [Fact]
    public async Task PythonAgent_ActualToolCalls_ArePersistedNotSynthetic()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var targetDateLocal = GetNextWeekdayLocal();

        var pythonToolCalls = new List<PythonAgentToolCallDto>
        {
            new()
            {
                ToolName = "GetTechnicianCalendar",
                InputParams = MakeJsonElement(new { technician_id = tech.Id.ToString() }),
                OutputParams = MakeJsonElement(new { is_available = true, active_bookings = 2 }),
                ExecutionTimeMs = 12,
                Success = true
            },
            new()
            {
                ToolName = "GetBusinessHours",
                InputParams = MakeJsonElement(new { date = targetDateLocal.ToString("yyyy-MM-dd") }),
                OutputParams = MakeJsonElement(new { open = "08:00", close = "17:00", is_working_day = true }),
                ExecutionTimeMs = 3,
                Success = true
            },
            new()
            {
                ToolName = "CreateScheduleProposal",
                InputParams = MakeJsonElement(new { duration = 60 }),
                OutputParams = MakeJsonElement(new { proposed_start = "2026-10-08T14:00:00Z" }),
                ExecutionTimeMs = 25,
                Success = true
            }
        };

        var mockPython = CreateMockPythonClient(new PythonSchedulingResult
        {
            Success = true,
            Status = "SUCCESS",
            OutputData = MakeJsonElement(new
            {
                proposed_start_time = LocalToUtc(targetDateLocal.AddHours(14)).ToString("o"),
                proposed_end_time = LocalToUtc(targetDateLocal.AddHours(15)).ToString("o"),
                llm_used = false,
                scheduling_path = "deterministic"
            }),
            ToolCalls = pythonToolCalls,
            ExecutionMode = "deterministic_fallback",
            LlmUsed = false,
            SchedulingPath = "deterministic"
        });

        var woService = CreateWorkOrderServiceWithPython(ctx, mockPython, out _);

        var startTimeLocal = targetDateLocal.AddHours(10);
        var endTimeLocal = targetDateLocal.AddHours(11);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Python Tool Calls Persistence",
            Description = "High priority to trigger approval workflow and tool call persistence",
            Priority = "High",
            ScheduledStartTime = startTimeLocal,
            ScheduledEndTime = endTimeLocal,
            EstimatedDurationMinutes = 60,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        await woService.CreateWorkOrderAsync(createDto, manager.Id);

        var workflow = await ctx.AgentWorkflows
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .FirstOrDefaultAsync(w => w.RequestId == request.Id);

        Assert.NotNull(workflow);
        var step = workflow.Steps.First();

        Assert.Equal(3, step.ToolCalls.Count);
        Assert.Contains(step.ToolCalls, tc => tc.ToolName == "GetTechnicianCalendar" && tc.ExecutionTimeMs == 12);
        Assert.Contains(step.ToolCalls, tc => tc.ToolName == "GetBusinessHours" && tc.ExecutionTimeMs == 3);
        Assert.Contains(step.ToolCalls, tc => tc.ToolName == "CreateScheduleProposal" && tc.ExecutionTimeMs == 25);
    }
    [Fact]
    public async Task PythonAgent_HighPriority_StillRequiresManagerApproval()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var targetDateLocal = GetNextWeekdayLocal();

        var mockPython = CreateMockPythonClient(new PythonSchedulingResult
        {
            Success = true,
            Status = "SUCCESS",
            OutputData = MakeJsonElement(new
            {
                proposed_start_time = LocalToUtc(targetDateLocal.AddHours(10)).ToString("o"),
                proposed_end_time = LocalToUtc(targetDateLocal.AddHours(11)).ToString("o"),
                llm_used = true
            }),
            ToolCalls = new List<PythonAgentToolCallDto>
            {
                new() { ToolName = "ValidateSchedule", Success = true, ExecutionTimeMs = 8 }
            },
            ExecutionMode = "llm_assisted",
            LlmUsed = true
        });

        var woService = CreateWorkOrderServiceWithPython(ctx, mockPython, out _);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "High Priority With Python Agent",
            Description = "High priority must still require manager approval regardless of Python agent",
            Priority = "High",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.Equal("PendingManagerApproval", result.Status);
        Assert.False(result.ConflictDetected);
        Assert.Contains("Python Scheduling Agent", result.AiDecisionSummary);
        Assert.Contains("high", result.AiDecisionSummary, StringComparison.OrdinalIgnoreCase);

        var workflow = await ctx.AgentWorkflows
            .Include(w => w.ApprovalActions)
            .FirstOrDefaultAsync(w => w.RequestId == request.Id);
        Assert.NotNull(workflow);
        Assert.Equal("WaitingForApproval", workflow.Status.ToString());
        Assert.NotEmpty(workflow.ApprovalActions);
    }

    [Fact]
    public async Task PythonAgent_ReceivesRealDbData_ExistingBookingsAndBusinessHours()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var targetDateLocal = GetNextWeekdayLocal();
        var targetDateUtc = GetNextWeekdayUtc();

        await ctx.Set<WorkOrder>().AddAsync(new WorkOrder
        {
            WorkOrderNumber = "WO-EXISTING-PYTHON",
            Title = "Existing Booking For Python Test",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = targetDateUtc.AddHours(8),
            ScheduledEndTime = targetDateUtc.AddHours(10),
            EstimatedDurationMinutes = 120
        });

        var dayOfWeek = (int)targetDateLocal.DayOfWeek;
        await ctx.Set<BusinessHours>().AddAsync(new BusinessHours
        {
            DayOfWeek = dayOfWeek,
            OpenTime = new TimeSpan(8, 0, 0),
            CloseTime = new TimeSpan(17, 0, 0),
            IsWorkingDay = true
        });

        await ctx.SaveChangesAsync();

        SchedulingAgentInvocation? capturedInvocation = null;
        var mockPython = new Mock<IPythonSchedulingAgentClient>();
        mockPython.Setup(c => c.ExecuteSchedulingAgentAsync(
                It.IsAny<SchedulingAgentInvocation>(),
                It.IsAny<CancellationToken>()))
            .Callback<SchedulingAgentInvocation, CancellationToken>((inv, _) => capturedInvocation = inv)
            .ReturnsAsync(new PythonSchedulingResult
            {
                Success = false,
                Status = "DETERMINISTIC",
                ToolCalls = new(),
                ExecutionMode = "deterministic_fallback"
            });

        var woService = CreateWorkOrderServiceWithPython(ctx, mockPython, out _);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Real DB Data Test",
            Description = "Verify real DB data is passed to Python agent",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60,
            SLADeadline = targetDateLocal.AddDays(5)
        };

        await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.NotNull(capturedInvocation);
        Assert.Equal(request.Id, capturedInvocation.RequestId);
        Assert.Equal(tech.Id, capturedInvocation.TechnicianId);
        Assert.NotNull(capturedInvocation.ExistingBookings);
        Assert.NotNull(capturedInvocation.BusinessHours);

        var bookingsJson = capturedInvocation.ExistingBookings.Value.GetRawText();
        Assert.Contains("Existing Booking For Python Test", bookingsJson);

        var bhJson = capturedInvocation.BusinessHours.Value.GetRawText();
        Assert.Contains("08:00:00", bhJson);
        Assert.Contains("17:00:00", bhJson);
    }
    // ============================================================
    // BUG FIX REGRESSION — SLA Default + Technician Availability
    // ============================================================

    [Fact]
    public async Task CreateWorkOrderAsync_MissingSlaDeadline_ShouldComputeDefaultFromSlaConfig()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();
        var startTimeLocal = targetDateLocal.AddHours(10);
        var endTimeLocal = targetDateLocal.AddHours(11);

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "SLA Default Test",
            Description = "No SLA deadline provided — should compute from SLAConfig",
            Priority = "Medium",
            ScheduledStartTime = startTimeLocal,
            ScheduledEndTime = endTimeLocal,
            EstimatedDurationMinutes = 60,
            SLADeadline = null
        };

        var beforeCall = DateTime.UtcNow;
        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.NotNull(result);
        var savedWo = await ctx.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == result.Id);
        Assert.NotNull(savedWo);
        Assert.NotNull(savedWo.SLADeadline);
        Assert.True(savedWo.SLADeadline.Value > beforeCall);
        // Medium priority SLA config has ResolutionTimeHours = 120
        Assert.True(savedWo.SLADeadline.Value <= beforeCall.AddHours(121));
    }
    [Fact]
    public async Task AssignTechnicianAsync_ShouldNotSetTechnicianUnavailable()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        Assert.True(tech.IsAvailable);

        var httpClient = new HttpClient();
        var techService = new TechnicianService(ctx, httpClient);

        await techService.AssignTechnicianAsync(request.Id, tech.Id);

        var updatedTech = await ctx.Technicians.FirstOrDefaultAsync(t => t.Id == tech.Id);
        Assert.NotNull(updatedTech);
        Assert.True(updatedTech.IsAvailable);
    }

    [Fact]
    public async Task ExistingWorkOrder_ShouldBlockOnlyConflictingSlot_NotAllSlots()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var targetDate = GetNextWeekdayUtc();

        await ctx.Set<WorkOrder>().AddAsync(new WorkOrder
        {
            WorkOrderNumber = "WO-BLOCK-001",
            Title = "Morning Block",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = targetDate.AddHours(9),
            ScheduledEndTime = targetDate.AddHours(11),
            EstimatedDurationMinutes = 120
        });
        await ctx.SaveChangesAsync();

        var conflictResult = await schedService.ValidateScheduleAsync(
            tech.Id, targetDate.AddHours(10), targetDate.AddHours(12), 120, "Medium");
        Assert.False(conflictResult.IsConflictFree);

        var freeResult = await schedService.ValidateScheduleAsync(
            tech.Id, targetDate.AddHours(3), targetDate.AddHours(4), 60, "Medium");
        Assert.True(freeResult.IsConflictFree);
        Assert.True(freeResult.IsValid);
    }

    [Fact]
    public async Task AlternativeSlot_ShouldBeFound_WhenRequestedSlotConflicts()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();
        var targetDateUtc = GetNextWeekdayUtc();

        await ctx.Set<WorkOrder>().AddAsync(new WorkOrder
        {
            WorkOrderNumber = "WO-ALT-001",
            Title = "Existing Block",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = targetDateUtc.AddHours(10),
            ScheduledEndTime = targetDateUtc.AddHours(12),
            EstimatedDurationMinutes = 120
        });

        var dayOfWeek = (int)targetDateUtc.DayOfWeek;
        await ctx.Set<BusinessHours>().AddAsync(new BusinessHours
        {
            DayOfWeek = dayOfWeek,
            OpenTime = new TimeSpan(8, 0, 0),
            CloseTime = new TimeSpan(17, 0, 0),
            IsWorkingDay = true
        });
        await ctx.SaveChangesAsync();

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Alternative Slot Test",
            Description = "Conflicting slot should find alternative",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(12),
            EstimatedDurationMinutes = 120
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.NotNull(result);
        Assert.True(result.ScheduledStartTime.HasValue);
        Assert.True(result.ScheduledEndTime.HasValue);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_MediumPriority_ValidSlot_ShouldScheduleDirectly()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Medium Priority Direct Schedule",
            Description = "Medium priority with valid slot should schedule directly",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.Equal("Scheduled", result.Status);
        Assert.False(result.ConflictDetected);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_CriticalPriority_ValidSlot_ShouldRequireManagerApproval()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();

        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "Critical Priority Approval",
            Description = "Critical priority should require manager approval",
            Priority = "Critical",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.Equal("PendingManagerApproval", result.Status);
        Assert.False(result.ConflictDetected);

        var workflow = await ctx.AgentWorkflows
            .FirstOrDefaultAsync(w => w.RequestId == request.Id);
        Assert.NotNull(workflow);
        Assert.Equal("WaitingForApproval", workflow.Status.ToString());
    }
    // ============================================================
    // SLA CALCULATION — Priority Assessment vs SLAConfiguration
    // ============================================================

    [Fact]
    public async Task SlaCalculation_ShouldUsePriorityAssessment_WhenAvailable()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var assessment = new PriorityAssessment
        {
            RequestId = request.Id,
            Priority = "Low",
            ResolutionTimeHours = 48,
            ResponseTimeHours = 8,
            AssessedBy = "TestPriorityAgent"
        };
        await ctx.Set<PriorityAssessment>().AddAsync(assessment);

        var requestCreatedAt = new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc);
        request.CreatedAt = requestCreatedAt;
        await ctx.SaveChangesAsync();

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();
        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "SLA From Priority Assessment",
            Description = "Should use PriorityAssessment.ResolutionTimeHours",
            Priority = "Low",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60,
            SLADeadline = null
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        Assert.NotNull(result);
        var savedWo = await ctx.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == result.Id);
        Assert.NotNull(savedWo?.SLADeadline);

        var expectedSla = requestCreatedAt.AddHours(48);
        Assert.Equal(expectedSla, savedWo.SLADeadline.Value);
    }
    [Fact]
    public async Task SlaDeadline_ShouldBeStoredInUtc()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var requestCreatedAt = new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc);
        request.CreatedAt = requestCreatedAt;
        await ctx.SaveChangesAsync();

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();
        var createDto = new WorkOrderCreateDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Title = "SLA UTC Storage Test",
            Description = "SLA deadline should be stored in UTC",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(10),
            ScheduledEndTime = targetDateLocal.AddHours(11),
            EstimatedDurationMinutes = 60,
            SLADeadline = null
        };

        var result = await woService.CreateWorkOrderAsync(createDto, manager.Id);

        var savedWo = await ctx.Set<WorkOrder>().FirstOrDefaultAsync(w => w.Id == result.Id);
        Assert.NotNull(savedWo?.SLADeadline);
        Assert.Equal(DateTimeKind.Utc, savedWo.SLADeadline.Value.Kind);

        var localDisplay = TimeZoneInfo.ConvertTimeFromUtc(savedWo.SLADeadline.Value, FacilityTimeZone);
        Assert.True(localDisplay > requestCreatedAt);
    }
    [Fact]
    public async Task ScheduleBeforeSlaDeadline_ShouldBeSlaCompliant()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var targetDateLocal = GetNextWeekdayLocal();
        var startTimeLocal = targetDateLocal.AddHours(10);
        var endTimeLocal = targetDateLocal.AddHours(11);
        var slaDeadlineLocal = targetDateLocal.AddDays(3).AddHours(17);

        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id, startTimeLocal, endTimeLocal, 60, "Medium", slaDeadlineLocal);

        Assert.True(valResult.IsValid);
        Assert.True(valResult.IsSlaCompliant);
    }

    // ============================================================
    // WORK ORDER UPDATE / EDIT — CRUD Tests
    // ============================================================

    private async Task<WorkOrder> SeedScheduledWorkOrder(FixFlowDbContext ctx, Technician tech, MaintenanceRequest request)
    {
        var targetDateLocal = GetNextWeekdayLocal();
        var startTimeUtc = LocalToUtc(targetDateLocal.AddHours(10));
        var endTimeUtc = LocalToUtc(targetDateLocal.AddHours(11));

        var wo = new WorkOrder
        {
            WorkOrderNumber = "WO-EDIT-001",
            Title = "Editable Work Order",
            Description = "Original description",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Priority = WorkOrderPriority.Medium,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = startTimeUtc,
            ScheduledEndTime = endTimeUtc,
            EstimatedDurationMinutes = 60,
            SLADeadline = LocalToUtc(targetDateLocal.AddDays(5).AddHours(17))
        };
        await ctx.Set<WorkOrder>().AddAsync(wo);
        await ctx.SaveChangesAsync();
        return wo;
    }

    [Fact]
    public async Task UpdateWorkOrderAsync_ShouldSucceed_WithValidData()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = await SeedScheduledWorkOrder(ctx, tech, request);

        var targetDateLocal = GetNextWeekdayLocal();
        var newStartLocal = targetDateLocal.AddHours(13);
        var newEndLocal = targetDateLocal.AddHours(14);

        var updateDto = new WorkOrderUpdateDto
        {
            Title = "Updated Title",
            Description = "Updated description",
            Priority = "Medium",
            ScheduledStartTime = newStartLocal,
            ScheduledEndTime = newEndLocal,
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        var result = await woService.UpdateWorkOrderAsync(wo.Id, updateDto, manager.Id);

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(LocalToUtc(newStartLocal), result.ScheduledStartTime);
        Assert.Equal(LocalToUtc(newEndLocal), result.ScheduledEndTime);
    }

    [Fact]
    public async Task UpdateWorkOrderAsync_ShouldReject_WhenConflictingSchedule()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = await SeedScheduledWorkOrder(ctx, tech, request);

        // Create a blocking work order at 2:00 PM - 3:00 PM local time (converted to UTC)
        var targetDateLocal = GetNextWeekdayLocal();
        var blockingStartUtc = LocalToUtc(targetDateLocal.AddHours(14));
        var blockingEndUtc = LocalToUtc(targetDateLocal.AddHours(15));
        await ctx.Set<WorkOrder>().AddAsync(new WorkOrder
        {
            WorkOrderNumber = "WO-BLOCK-EDIT",
            Title = "Blocking Job",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = blockingStartUtc,
            ScheduledEndTime = blockingEndUtc,
            EstimatedDurationMinutes = 60
        });
        await ctx.SaveChangesAsync();

        // Try to update to the same time slot (2:00 PM - 3:00 PM local)
        var updateDto = new WorkOrderUpdateDto
        {
            Title = wo.Title,
            Description = wo.Description ?? "",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(14),
            ScheduledEndTime = targetDateLocal.AddHours(15),
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            woService.UpdateWorkOrderAsync(wo.Id, updateDto, manager.Id));
    }

    [Fact]
    public async Task UpdateWorkOrderAsync_ShouldReject_WhenOutsideBusinessHours()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = await SeedScheduledWorkOrder(ctx, tech, request);

        var targetDateLocal = GetNextWeekdayLocal();
        var updateDto = new WorkOrderUpdateDto
        {
            Title = wo.Title,
            Description = wo.Description ?? "",
            Priority = "Medium",
            ScheduledStartTime = targetDateLocal.AddHours(22),
            ScheduledEndTime = targetDateLocal.AddHours(23),
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            woService.UpdateWorkOrderAsync(wo.Id, updateDto, manager.Id));
    }
    [Fact]
    public async Task UpdateWorkOrderAsync_HighPriority_ShouldRequireReApproval()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = await SeedScheduledWorkOrder(ctx, tech, request);
        Assert.Equal(WorkOrderStatus.Scheduled, wo.Status);

        var targetDateLocal = GetNextWeekdayLocal();
        var updateDto = new WorkOrderUpdateDto
        {
            Title = wo.Title,
            Description = wo.Description ?? "",
            Priority = "High",
            ScheduledStartTime = targetDateLocal.AddHours(13),
            ScheduledEndTime = targetDateLocal.AddHours(14),
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        var result = await woService.UpdateWorkOrderAsync(wo.Id, updateDto, manager.Id);

        Assert.Equal("PendingManagerApproval", result.Status);

        var history = await ctx.Set<WorkOrderStatusHistory>()
            .Where(h => h.WorkOrderId == wo.Id)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.Equal(WorkOrderStatus.Scheduled, history.PreviousStatus);
        Assert.Equal(WorkOrderStatus.PendingManagerApproval, history.NewStatus);
    }

    [Fact]
    public async Task UpdateWorkOrderAsync_ShouldRejectCompletedWorkOrder()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = new WorkOrder
        {
            WorkOrderNumber = "WO-COMPLETED-EDIT",
            Title = "Already Completed",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Completed
        };
        await ctx.Set<WorkOrder>().AddAsync(wo);
        await ctx.SaveChangesAsync();

        var updateDto = new WorkOrderUpdateDto
        {
            Title = "Trying to edit completed",
            Description = "",
            Priority = "Medium",
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            woService.UpdateWorkOrderAsync(wo.Id, updateDto, manager.Id));
    }
    [Fact]
    public async Task UpdateWorkOrderAsync_ShouldRecordAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var wo = await SeedScheduledWorkOrder(ctx, tech, request);

        var updateDto = new WorkOrderUpdateDto
        {
            Title = "Audit Test",
            Description = "",
            Priority = "Medium",
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        await woService.UpdateWorkOrderAsync(wo.Id, updateDto, manager.Id);

        mockAudit.Verify(a => a.LogAsync(
            manager.Id,
            "WorkOrderUpdated",
            "WorkOrder",
            wo.Id.ToString(),
            It.IsAny<string>(),
            "127.0.0.1"), Times.Once);
    }

    [Fact]
    public async Task UpdateWorkOrderAsync_ShouldThrowNotFound_WhenWorkOrderDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        var woService = new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);

        var updateDto = new WorkOrderUpdateDto
        {
            Title = "Ghost Edit",
            Description = "",
            Priority = "Medium",
            EstimatedDurationMinutes = 60,
            TechnicianId = tech.Id
        };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            woService.UpdateWorkOrderAsync(Guid.NewGuid(), updateDto, manager.Id));
    }

}
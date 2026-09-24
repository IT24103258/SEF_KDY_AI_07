using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using FixFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FixFlow.Tests;

public class WorkOrderTests
{
    private FixFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"FixFlow_TestDb_{Guid.NewGuid()}")
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
            RequesterId = requester.Id
        };
        await context.MaintenanceRequests.AddAsync(request);

        // Seed SLAs across standard levels to prevent SLA evaluation missing configurations
        var slas = new List<SLAConfiguration>
        {
            new SLAConfiguration { PriorityLevel = "High", ResponseTimeHours = 2, ResolutionTimeHours = 24 },
            new SLAConfiguration { PriorityLevel = "Medium", ResponseTimeHours = 4, ResolutionTimeHours = 48 },
            new SLAConfiguration { PriorityLevel = "Low", ResponseTimeHours = 8, ResolutionTimeHours = 72 }
        };
        await context.SLAConfigurations.AddRangeAsync(slas);

        await context.SaveChangesAsync();
        return (context, tech, request, manager, requester);
    }

    [Fact]
    public async Task CreateConflictFreeWorkOrderAsync_ShouldProposeValidSlot_WhenNoConflictsExist()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();

        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        // Dynamically compute the next weekday at 10:00 AM to guarantee valid business hours
        var targetDate = DateTime.UtcNow.Date.AddDays(1);
        while (targetDate.DayOfWeek == DayOfWeek.Saturday || targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDate = targetDate.AddDays(1);
        }
        var validBusinessSlot = targetDate.AddHours(10); // 10:00 AM weekday

        var reqDto = new ScheduleRequestDto
        {
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Priority = "High",
            EstimatedDurationMinutes = 120,
            PreferredStartTime = validBusinessSlot
        };

        var proposal = await schedService.CreateConflictFreeWorkOrderAsync(reqDto, requester.Id);

        Assert.NotNull(proposal);
        Assert.False(proposal.ConflictDetected);
        Assert.True(proposal.WithinBusinessHours);
        Assert.True(proposal.WithinTechnicianAvailability);
        Assert.True(proposal.SlaCompliant);
        Assert.Equal("PendingManagerApproval", proposal.ProposalStatus);
        Assert.NotEmpty(proposal.DecisionSummary);
    }

    [Fact]
    public async Task ValidateScheduleAsync_ShouldDetectConflict_WhenDirectOverlapExists()
    {
        using var context = CreateInMemoryDbContext();
        var (ctx, tech, request, manager, requester) = await SeedBasicTestData(context);

        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();

        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);

        var today = DateTime.UtcNow.Date.AddDays(1);
        var existingBooking = new WorkOrder
        {
            WorkOrderNumber = "WO-TEST-001",
            Title = "Existing Morning Job",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.Scheduled,
            ScheduledStartTime = today.AddHours(9),
            ScheduledEndTime = today.AddHours(11),
            EstimatedDurationMinutes = 120
        };
        await ctx.Set<WorkOrder>().AddAsync(existingBooking);
        await ctx.SaveChangesAsync();

        // Overlapping proposed slot: 10:00 - 12:00
        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id,
            today.AddHours(10),
            today.AddHours(12),
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

        // Night time slot: 22:00 - 23:30
        var today = DateTime.UtcNow.Date.AddDays(1);
        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id,
            today.AddHours(22),
            today.AddHours(23).AddMinutes(30),
            90,
            "Medium");

        Assert.False(valResult.IsValid);
        Assert.False(valResult.IsWithinBusinessHours);
        Assert.Contains(valResult.ValidationErrors, e => e.Contains("outside operational business hours", StringComparison.OrdinalIgnoreCase));
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

        var today = DateTime.UtcNow.Date.AddDays(1);
        var valResult = await schedService.ValidateScheduleAsync(
            tech.Id,
            today.AddHours(10),
            today.AddHours(12),
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

        // Attempting to jump directly from Draft to Completed should throw InvalidOperationException
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

        var today = DateTime.UtcNow.Date.AddDays(1);
        var pendingWo = new WorkOrder
        {
            WorkOrderNumber = "WO-PENDING-001",
            Title = "Pending Manager Sign-off",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.PendingManagerApproval,
            ScheduledStartTime = today.AddHours(14),
            ScheduledEndTime = today.AddHours(16),
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

        // Verify notification sent to technician
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
}
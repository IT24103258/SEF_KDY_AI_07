using System.Security.Claims;
using FixFlow.Api.Controllers;
using FixFlow.Api.Data;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using FixFlow.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FixFlow.Tests;

public class FileStorageTests : IDisposable
{
    private readonly string _tempDir;

    public FileStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fixflow-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private IFileStorageService CreateStorage(long? maxSizeBytes = null)
    {
        var settings = new Dictionary<string, string?> { ["FileStorage:UploadDirectory"] = _tempDir };
        if (maxSizeBytes.HasValue)
        {
            settings["FileStorage:MaxSizeBytes"] = maxSizeBytes.Value.ToString();
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new LocalFileStorageService(configuration);
    }

    private static Stream StreamOf(byte[] bytes) => new MemoryStream(bytes);

    // ---------- LocalFileStorageService ----------

    [Fact]
    public async Task UploadFileAsync_SanitizesUnsafeFileName_AndStoresUnderGeneratedKey()
    {
        var storage = CreateStorage();
        var content = System.Text.Encoding.UTF8.GetBytes("hello");

        var key = await storage.UploadFileAsync(StreamOf(content), "..\\..\\evil<script>.jpg");

        Assert.DoesNotContain("..", key);
        Assert.DoesNotContain(Path.DirectorySeparatorChar.ToString(), key);
        Assert.DoesNotContain("/", key);
        Assert.EndsWith(".jpg", key);
        Assert.True(File.Exists(Path.Combine(_tempDir, key)));
        Assert.False(File.Exists(Path.Combine(_tempDir, "evil<script>.jpg")));
    }

    [Fact]
    public async Task UploadFileAsync_RejectsEmptyFileName()
    {
        var storage = CreateStorage();

        await Assert.ThrowsAsync<ValidationException>(() =>
            storage.UploadFileAsync(StreamOf(new byte[] { 1, 2 }), "   "));
    }

    [Fact]
    public async Task UploadFileAsync_EnforcesMaxSizeBytes_AndLeavesNoPartialFile()
    {
        var storage = CreateStorage(maxSizeBytes: 4);

        await Assert.ThrowsAsync<ValidationException>(() =>
            storage.UploadFileAsync(StreamOf(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }), "photo.jpg"));

        Assert.Empty(Directory.GetFiles(_tempDir));
    }

    [Fact]
    public async Task UploadThenGet_RoundTripsContent()
    {
        var storage = CreateStorage();
        var content = System.Text.Encoding.UTF8.GetBytes("jpeg-bytes-here");

        var key = await storage.UploadFileAsync(StreamOf(content), "photo.jpg");

        using var stream = await storage.GetFileAsync(key);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(content, ms.ToArray());
    }

    [Fact]
    public async Task GetFileAsync_StillResolvesLegacyUnderscoreKeys()
    {
        var storage = CreateStorage();
        var legacyName = $"{Guid.NewGuid()}_old-photo.jpg";
        await File.WriteAllBytesAsync(Path.Combine(_tempDir, legacyName), new byte[] { 9, 9 });

        using var stream = await storage.GetFileAsync(legacyName);

        Assert.NotNull(stream);
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("..\\secret.txt")]
    [InlineData("sub/../../../secret.txt")]
    public async Task GetFileAsync_RejectsPathTraversalKeys(string fileKey)
    {
        var storage = CreateStorage();

        await Assert.ThrowsAsync<NotFoundException>(() => storage.GetFileAsync(fileKey));
    }

    [Fact]
    public async Task GetFileAsync_ReturnsNotFound_WhenFileMissing()
    {
        var storage = CreateStorage();

        await Assert.ThrowsAsync<NotFoundException>(() => storage.GetFileAsync($"{Guid.NewGuid()}.jpg"));
    }

    // ---------- WorkOrderService evidence authorization ----------

    private async Task<(FixFlowDbContext ctx, WorkOrder workOrder, Technician tech, User techUser, User adminUser, User otherTechUser)> SeedWorkOrderWithEvidence()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"FixFlow_TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new FixFlowDbContext(options);
        context.Database.EnsureCreated();

        var roleAdmin = new Role { Name = "Administrator", Description = "Admin role" };
        var roleTech = new Role { Name = "Technician", Description = "Tech role" };
        var roleReq = new Role { Name = "Requester", Description = "Requester role" };
        await context.Roles.AddRangeAsync(roleAdmin, roleTech, roleReq);

        var techUser = new User { Email = "tech@fixflow.local", FirstName = "Tech", LastName = "User", RoleId = roleTech.Id };
        var adminUser = new User { Email = "admin@fixflow.local", FirstName = "Admin", LastName = "User", RoleId = roleAdmin.Id };
        var otherTechUser = new User { Email = "tech2@fixflow.local", FirstName = "Other", LastName = "Tech", RoleId = roleTech.Id };
        var requester = new User { Email = "req@fixflow.local", FirstName = "Resident", LastName = "Requester", RoleId = roleReq.Id };
        await context.Users.AddRangeAsync(techUser, adminUser, otherTechUser, requester);

        var tech = new Technician { UserId = techUser.Id, EmployeeId = "TECH-001", Specialization = "Electrical", IsAvailable = true, User = techUser };
        var otherTech = new Technician { UserId = otherTechUser.Id, EmployeeId = "TECH-002", Specialization = "Plumbing", IsAvailable = true, User = otherTechUser };
        await context.Technicians.AddRangeAsync(tech, otherTech);

        var loc = new Location { Name = "Tower A - Unit 305", Building = "Tower A", Floor = "3", Room = "305" };
        await context.Locations.AddAsync(loc);

        var request = new MaintenanceRequest
        {
            RequestNumber = "REQ-2026-0001",
            Title = "Ceiling Light Short Circuit",
            Description = "Light sparked.",
            Status = RequestStatus.Matched,
            LocationId = loc.Id,
            RequesterId = requester.Id,
            CreatedAt = DateTime.UtcNow
        };
        await context.MaintenanceRequests.AddAsync(request);
        await context.SaveChangesAsync();

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = "WO-202609-0001",
            Title = "Ceiling Leak Inspection",
            RequestId = request.Id,
            TechnicianId = tech.Id,
            Status = WorkOrderStatus.InProgress,
            EstimatedDurationMinutes = 60
        };
        workOrder.Evidence.Add(new CompletionEvidence
        {
            FileKey = $"{Guid.NewGuid()}_photo.jpg",
            OriginalFileName = "photo.jpg",
            UploadedById = techUser.Id,
            UploadedAt = DateTime.UtcNow
        });
        await context.Set<WorkOrder>().AddAsync(workOrder);
        await context.SaveChangesAsync();

        return (context, workOrder, tech, techUser, adminUser, otherTechUser);
    }

    private static WorkOrderService CreateWorkOrderService(FixFlowDbContext ctx)
    {
        var mockAudit = new Mock<IAuditLogService>();
        var mockNotify = new Mock<INotificationService>();
        var schedService = new SchedulingService(ctx, mockAudit.Object, mockNotify.Object);
        return new WorkOrderService(ctx, schedService, mockAudit.Object, mockNotify.Object);
    }

    [Fact]
    public async Task EnsureEvidenceUploadAllowedAsync_AllowsAssignedTechnician()
    {
        var (ctx, workOrder, tech, techUser, _, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);

        await service.EnsureEvidenceUploadAllowedAsync(workOrder.Id, techUser.Id, "Technician");
    }

    [Fact]
    public async Task EnsureEvidenceUploadAllowedAsync_ThrowsForbidden_ForOtherTechnician()
    {
        var (ctx, workOrder, _, _, _, otherTechUser) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.EnsureEvidenceUploadAllowedAsync(workOrder.Id, otherTechUser.Id, "Technician"));
    }

    [Fact]
    public async Task EnsureEvidenceUploadAllowedAsync_AllowsAdministrator()
    {
        var (ctx, workOrder, _, _, adminUser, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);

        await service.EnsureEvidenceUploadAllowedAsync(workOrder.Id, adminUser.Id, "Administrator");
    }

    [Fact]
    public async Task EnsureEvidenceUploadAllowedAsync_ThrowsNotFound_ForMissingWorkOrder()
    {
        var (ctx, _, _, _, _, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.EnsureEvidenceUploadAllowedAsync(Guid.NewGuid(), Guid.NewGuid(), "Technician"));
    }

    [Fact]
    public async Task GetEvidenceFileAsync_ReturnsFileKey_ForAssignedTechnician()
    {
        var (ctx, workOrder, tech, techUser, _, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);
        var evidenceId = workOrder.Evidence.First().Id;

        var dto = await service.GetEvidenceFileAsync(workOrder.Id, evidenceId, techUser.Id, "Technician");

        Assert.Equal(evidenceId, dto.Id);
        Assert.Equal(workOrder.Evidence.First().FileKey, dto.FileKey);
        Assert.Equal("photo.jpg", dto.OriginalFileName);
    }

    [Fact]
    public async Task GetEvidenceFileAsync_ThrowsForbidden_ForUnassignedTechnician()
    {
        var (ctx, workOrder, _, _, _, otherTechUser) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);
        var evidenceId = workOrder.Evidence.First().Id;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetEvidenceFileAsync(workOrder.Id, evidenceId, otherTechUser.Id, "Technician"));
    }

    [Fact]
    public async Task GetEvidenceFileAsync_ThrowsNotFound_ForForeignEvidenceId()
    {
        var (ctx, workOrder, _, techUser, _, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetEvidenceFileAsync(workOrder.Id, Guid.NewGuid(), techUser.Id, "Technician"));
    }

    [Fact]
    public async Task GetEvidenceFileAsync_AllowsRequesterWhoOwnsTheRequest()
    {
        var (ctx, workOrder, _, _, _, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);
        var requester = ctx.Users.First(u => u.Email == "req@fixflow.local");
        var evidenceId = workOrder.Evidence.First().Id;

        var dto = await service.GetEvidenceFileAsync(workOrder.Id, evidenceId, requester.Id, "Requester");

        Assert.Equal(evidenceId, dto.Id);
    }

    [Fact]
    public async Task GetEvidenceFileAsync_ThrowsForbidden_ForOtherRequester()
    {
        var (ctx, workOrder, _, _, _, _) = await SeedWorkOrderWithEvidence();
        var service = CreateWorkOrderService(ctx);
        var stranger = ctx.Users.First(u => u.Email == "admin@fixflow.local");
        var evidenceId = workOrder.Evidence.First().Id;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetEvidenceFileAsync(workOrder.Id, evidenceId, stranger.Id, "Requester"));
    }

    // ---------- WorkOrdersController upload validation ----------

    private static WorkOrdersController CreateController(Mock<IWorkOrderService> woMock, Mock<IFileStorageService> storageMock, Guid userId, string role)
    {
        var controller = new WorkOrdersController(woMock.Object, storageMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                }, "TestAuth"))
            }
        };
        return controller;
    }

    private static IFormFile FormFile(string fileName, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    [Fact]
    public async Task UploadPhoto_ReturnsBadRequest_WhenNoFileProvided()
    {
        var woMock = new Mock<IWorkOrderService>();
        var storageMock = new Mock<IFileStorageService>();
        var controller = CreateController(woMock, storageMock, Guid.NewGuid(), "Administrator");

        var result = await controller.UploadPhoto(Guid.NewGuid(), null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadPhoto_ReturnsBadRequest_ForUnsupportedFileType()
    {
        var woMock = new Mock<IWorkOrderService>();
        var storageMock = new Mock<IFileStorageService>();
        var controller = CreateController(woMock, storageMock, Guid.NewGuid(), "Administrator");

        var result = await controller.UploadPhoto(Guid.NewGuid(), FormFile("animation.gif", new byte[] { 1, 2, 3 }));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        storageMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_ChecksWorkOrderAuthorization_BeforeStoringFile()
    {
        var woMock = new Mock<IWorkOrderService>();
        var storageMock = new Mock<IFileStorageService>();
        storageMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), "photo.jpg"))
            .ReturnsAsync($"{Guid.NewGuid()}.jpg");
        var adminId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var controller = CreateController(woMock, storageMock, adminId, "Administrator");

        var result = await controller.UploadPhoto(workOrderId, FormFile("photo.jpg", new byte[] { 1, 2, 3 }));

        Assert.IsType<OkObjectResult>(result.Result);
        woMock.Verify(s => s.EnsureEvidenceUploadAllowedAsync(workOrderId, adminId, "Administrator"), Times.Once);
        storageMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), "photo.jpg"), Times.Once);
    }
}

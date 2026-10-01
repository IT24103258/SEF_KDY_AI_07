using System.Security.Claims;
using FixFlow.Api.Controllers;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using FixFlow.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FixFlow.Tests;

public class RequestServiceTests : IDisposable
{
    private readonly FixFlowDbContext _context;
    private readonly Mock<IClassificationAgentService> _mockAgent;
    private readonly RequestService _service;

    public RequestServiceTests()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new FixFlowDbContext(options);
        _mockAgent = new Mock<IClassificationAgentService>();
        var mockLogger = new Mock<ILogger<RequestService>>();
        var mockPriorityAgent = new Mock<IPriorityAgentService>();
        var mockFileStorage = new Mock<IFileStorageService>();

        _service = new RequestService(
            _context,
            _mockAgent.Object,
            mockPriorityAgent.Object,
            mockFileStorage.Object,
            mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private MaintenanceRequest SeedFullRequest(Guid userId, Guid reqId, string status = "Submitted")
    {
        var location = new Location { Id = Guid.NewGuid(), Name = "Loc" };
        var user = new User { Id = userId, FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "x", RoleId = Guid.NewGuid() };
        
        _context.Set<Location>().Add(location);
        _context.Set<User>().Add(user);
        
        var req = new MaintenanceRequest
        {
            Id = reqId,
            RequestNumber = "REQ-2026-0001",
            Title = "Test",
            Description = "Test",
            RequesterId = userId,
            LocationId = location.Id,
            Status = Enum.Parse<RequestStatus>(status)
        };
        _context.Set<MaintenanceRequest>().Add(req);
        _context.SaveChanges();
        return req;
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowForbidden_IfUserIsNotOwnerAndNotStaff()
    {
        var reqId = Guid.NewGuid();
        SeedFullRequest(Guid.NewGuid(), reqId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.GetByIdAsync(reqId, Guid.NewGuid(), "Requester"));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnData_IfUserIsOwner()
    {
        var userId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        SeedFullRequest(userId, reqId);

        var result = await _service.GetByIdAsync(reqId, userId, "Requester");

        Assert.NotNull(result);
        Assert.Equal(reqId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnData_IfUserIsStaff()
    {
        var reqId = Guid.NewGuid();
        SeedFullRequest(Guid.NewGuid(), reqId);

        var result = await _service.GetByIdAsync(reqId, Guid.NewGuid(), "Manager");

        Assert.NotNull(result);
        Assert.Equal(reqId, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_IfStatusNotSubmitted()
    {
        var userId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        SeedFullRequest(userId, reqId, "InReview");

        var dto = new UpdateRequestDto { Title = "Updated", Description = "Updated" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(reqId, dto, userId, "Requester"));
        Assert.Contains("Requests can only be edited while in 'Submitted' status", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateSequentialRequestNumber()
    {
        var userId = Guid.NewGuid();
        var year = DateTime.UtcNow.Year;
        
        var location = new Location { Id = Guid.NewGuid(), Name = "Loc" };
        var user = new User { Id = userId, FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "x", RoleId = Guid.NewGuid() };
        _context.Set<Location>().Add(location);
        _context.Set<User>().Add(user);
        
        _context.Set<MaintenanceRequest>().AddRange(
            new MaintenanceRequest { Id = Guid.NewGuid(), RequestNumber = $"REQ-{year}-0001", Title = "1", Description = "1", RequesterId = userId, LocationId = location.Id },
            new MaintenanceRequest { Id = Guid.NewGuid(), RequestNumber = $"REQ-{year}-0002", Title = "2", Description = "2", RequesterId = userId, LocationId = location.Id }
        );
        await _context.SaveChangesAsync();

        var dto = new CreateRequestDto
        {
            Title = "New Request",
            Description = "Description",
            LocationId = location.Id
        };

        var result = await _service.CreateAsync(dto, userId);
        
        var newReq = await _context.Set<MaintenanceRequest>().FindAsync(result.Id);
        Assert.NotNull(newReq);
        Assert.Equal($"REQ-{year}-0003", newReq.RequestNumber);
    }

    [Fact]
    public async Task ClassifyAsync_ShouldFallbackToUncategorized_IfAgentFails()
    {
        var reqId = Guid.NewGuid();
        SeedFullRequest(Guid.NewGuid(), reqId);

        _mockAgent.Setup(a => a.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((AgentClassificationResponseDto?)null);

        await _service.ClassifyAsync(reqId, Guid.NewGuid(), "Manager");

        var classification = await _context.Set<RequestClassification>().FirstOrDefaultAsync(c => c.MaintenanceRequestId == reqId);
        Assert.NotNull(classification);
        Assert.Equal("Uncategorized", classification.Category);
        Assert.True(classification.RequiresReview);
        
        var req = await _context.Set<MaintenanceRequest>().FindAsync(reqId);
        Assert.Equal(RequestStatus.InReview, req!.Status);
    }
}

public class RequestsControllerTests
{
    [Fact]
    public async Task GetMyRequests_ShouldUseUserIdFromJwtClaims()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IRequestService>();
        
        mockService.Setup(s => s.GetMyRequestsAsync(userId, It.IsAny<RequestListQueryDto>()))
                   .ReturnsAsync(new PagedResult<RequestSummaryDto> { Items = new List<RequestSummaryDto>() });

        var controller = new RequestsController(mockService.Object);
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Requester")
        }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var query = new RequestListQueryDto();
        var result = await controller.GetMyRequests(query);

        Assert.IsType<OkObjectResult>(result.Result);
        mockService.Verify(s => s.GetMyRequestsAsync(userId, query), Times.Once);
    }

    [Fact]
    public async Task CreateRequest_ShouldUseUserIdFromJwtClaims()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IRequestService>();
        
        mockService.Setup(s => s.CreateAsync(It.IsAny<CreateRequestDto>(), userId))
                   .ReturnsAsync(new RequestDetailDto { Id = Guid.NewGuid(), RequestNumber = "REQ-0001", Title = "T", Description = "D", Status = "Submitted", RequesterId = userId, RequesterName = "User", LocationId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, Classifications = new List<ClassificationResultDto>() });

        var controller = new RequestsController(mockService.Object);
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Requester")
        }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new CreateRequestDto { Title = "T", Description = "D", LocationId = Guid.NewGuid() };

        var result = await controller.CreateRequest(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        mockService.Verify(s => s.CreateAsync(dto, userId), Times.Once);
    }
}

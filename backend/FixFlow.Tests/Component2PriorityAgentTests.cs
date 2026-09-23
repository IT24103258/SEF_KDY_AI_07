using System.Net;
using System.Security.Claims;
using System.Text.Json;
using FixFlow.Api.Controllers;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using FixFlow.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FixFlow.Tests;

public class Component2PriorityAgentTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_handler(request));
        }
    }

    private FixFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FixFlowDbContext(options);
    }

    private async Task<MaintenanceRequest> CreateSampleRequestAsync(FixFlowDbContext context)
    {
        var role = new Role { Name = "Requester", Description = "Resident" };
        await context.Roles.AddAsync(role);

        var user = new User
        {
            Email = "resident@fixflow.local",
            PasswordHash = "hash",
            FirstName = "Alice",
            LastName = "Smith",
            PhoneNumber = "+94771112233",
            RoleId = role.Id
        };
        await context.Users.AddAsync(user);

        var loc = new Location
        {
            Name = "Tower A - Unit 305",
            Building = "Tower A",
            Floor = "Floor 3",
            Room = "Unit 305",
            Latitude = 6.9147,
            Longitude = 79.9733
        };
        await context.Locations.AddAsync(loc);

        var asset = new Asset
        {
            Name = "Lobby Elevator",
            AssetCode = "ASSET-ELV-01",
            Category = "Elevator/Lift",
            Criticality = "Critical",
            LocationId = loc.Id
        };
        await context.Assets.AddAsync(asset);

        var category = new IssueCategory
        {
            Name = "Elevator/Lift",
            Description = "Elevator issues",
            DefaultPriority = "Critical"
        };
        await context.IssueCategories.AddAsync(category);

        var req = new MaintenanceRequest
        {
            RequestNumber = "REQ-AGENT-001",
            Title = "Elevator door stuck with passengers inside",
            Description = "Passenger elevator on floor 3 is stuck and alarms sounding.",
            Status = RequestStatus.Submitted,
            LocationId = loc.Id,
            AssetId = asset.Id,
            CategoryId = category.Id,
            RequesterId = user.Id
        };
        await context.MaintenanceRequests.AddAsync(req);
        await context.SaveChangesAsync();

        return req;
    }

    private HttpResponseMessage CreateMockPythonOrchestratorResponse(
        int riskScore = 85,
        string riskLevel = "Critical",
        string priority = "Critical",
        bool escalationFlag = true)
    {
        var agentOutput = new PriorityAgentResultDto
        {
            AssetCriticality = "Critical",
            ImpactLevel = "Critical",
            LikelihoodLevel = "High",
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            Priority = priority,
            RecommendedResponseWindow = "Immediate (Within 1 hour)",
            Sla = new PriorityAgentSlaDto { ResponseHours = 1, ResolutionHours = 4 },
            EscalationFlag = escalationFlag,
            Explanation = "Evaluated Critical impact and High likelihood on Critical asset.",
            PriorityLevel = priority,
            TargetSlaHours = 4,
            HazardFlag = escalationFlag
        };

        var workflowResult = new PriorityAgentWorkflowExecutionResult
        {
            WorkflowId = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid().ToString(),
            Status = "COMPLETED",
            Steps = new List<PriorityAgentWorkflowStepResult>
            {
                new PriorityAgentWorkflowStepResult
                {
                    AgentName = "ClassificationAgent",
                    StepName = "Intake & Classification",
                    Status = "SUCCESS",
                    ValidationPassed = true
                },
                new PriorityAgentWorkflowStepResult
                {
                    AgentName = "PriorityAgent",
                    StepName = "Risk & Priority Assessment",
                    Status = "SUCCESS",
                    OutputData = agentOutput,
                    ValidationPassed = true
                }
            }
        };

        var json = JsonSerializer.Serialize(workflowResult);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task EvaluateAgentAsync_CallsPythonService_AndReturnsCalculatedResult()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(90, "Critical", "Critical", true));

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        var result = await agentService.EvaluateAgentAsync(req.Id);

        Assert.NotNull(result);
        Assert.Equal(90, result.RiskScore);
        Assert.Equal("Critical", result.Priority);
        Assert.Equal("Critical", result.RiskLevel);
        Assert.True(result.EscalationFlag);

        Assert.NotNull(mockHandler.LastRequest?.Content);
        var capturedBody = await mockHandler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"workflow_type\":\"Priority\"", capturedBody);
        Assert.Contains(req.Id.ToString(), capturedBody);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_PersistsAssessmentToPostgreSqlViaPriorityAssessmentService()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(75, "High", "High", false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        var assessmentDto = await agentService.EvaluateAndPersistAsync(req.Id, null, "PriorityAgent");

        Assert.NotNull(assessmentDto);
        Assert.Equal(75, assessmentDto.RiskScore);
        Assert.Equal("High", assessmentDto.Priority);
        Assert.Equal("PriorityAgent", assessmentDto.AssessedBy);

        // Verify entity persisted in DbContext
        var savedEntity = await context.Set<PriorityAssessment>().FirstOrDefaultAsync(p => p.RequestId == req.Id);
        Assert.NotNull(savedEntity);
        Assert.Equal(75, savedEntity.RiskScore);
        Assert.Equal("High", savedEntity.Priority);

        // Verify request status transitioned
        var updatedReq = await context.MaintenanceRequests.FindAsync(req.Id);
        Assert.NotNull(updatedReq);
        Assert.Equal(RequestStatus.PriorityAssigned, updatedReq.Status);

        // Verify repeated agent evaluation reuses existing assessment rather than inserting a duplicate
        var secondDto = await agentService.EvaluateAndPersistAsync(req.Id, null, "PriorityAgent");
        Assert.Equal(savedEntity.Id, secondDto.Id);
        var totalRows = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(1, totalRows);
    }

    [Fact]
    public async Task PriorityAgentController_AssessPriorityWithAgent_Returns200WithDto()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(80, "High", "High", false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());
        var controller = new PriorityAgentController(agentService, new NullLogger<PriorityAgentController>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, "manager@fixflow.local"),
                    new Claim(ClaimTypes.Role, "Manager")
                }, "TestAuth"))
            }
        };

        var actionResult = await controller.AssessPriorityWithAgent(req.Id, null);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PriorityAssessmentDto>>(okResult.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(80, response.Data.RiskScore);
        Assert.Equal("High", response.Data.Priority);
    }

    [Fact]
    public async Task PriorityAgentController_PreviewPriorityWithAgent_ReturnsCalculatedPreviewWithoutPersisting()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(60, "High", "High", false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());
        var controller = new PriorityAgentController(agentService, new NullLogger<PriorityAgentController>());

        var actionResult = await controller.PreviewPriorityWithAgent(req.Id, null);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PriorityAgentResultDto>>(okResult.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(60, response.Data.RiskScore);

        // Verify NOT persisted to DbContext
        var count = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(0, count);
    }
}


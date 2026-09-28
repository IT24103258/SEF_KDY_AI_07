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
    public async Task EvaluateAndPersistAsync_PersistsAssessmentViaPriorityAssessmentService_EFInMemory()
    {
        // NOTE (Issue 6): This is an EF Core InMemory provider test, NOT a live PostgreSQL
        // integration test. The name is intentionally honest about the provider. A guarded
        // real-PostgreSQL integration test lives in Component2PostgresIntegrationTests.cs and
        // is skipped unless FIXFLOW_INTEGRATION_CONNECTION is supplied.
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

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(65, "High", "High", false));
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
        Assert.Equal(65, response.Data.RiskScore);
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

        // Preview is an [Authorize]d endpoint; supply a principal so the RBAC override-stripping runs.
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

    [Fact]
    public async Task IssueA_NormalAssessmentAction_UsesAgenticWorkflowAndPersists()
    {
        // Issue A: Proves normal Component 2 assessment action uses the PriorityAgent agentic workflow
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        bool agentEndpointCalled = false;
        var mockHandler = new MockHttpMessageHandler(r =>
        {
            if (r.RequestUri?.AbsolutePath.Contains("execute") == true)
            {
                agentEndpointCalled = true;
            }
            return CreateMockPythonOrchestratorResponse(70, "High", "High", false);
        });

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
        Assert.True(agentEndpointCalled, "Normal assessment action MUST call the agentic orchestrator workflow");

        // Verify persisted to database
        var persisted = await context.Set<PriorityAssessment>().FirstOrDefaultAsync(p => p.RequestId == req.Id);
        Assert.NotNull(persisted);
        Assert.Equal("High", persisted.Priority);
    }

    [Fact]
    public async Task IssueB_Administrator_CanSupplyOverrides()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(r => CreateMockPythonOrchestratorResponse(90, "Critical", "Critical", true));
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
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim(ClaimTypes.Email, "admin@fixflow.local")
                }, "TestAuth"))
            }
        };

        var dto = new PriorityAgentEvaluationRequestDto
        {
            AssetCriticalityOverride = "Critical",
            ImpactOverride = "Critical",
            LikelihoodOverride = "Critical",
            HasSafetyHazard = true
        };

        await controller.AssessPriorityWithAgent(req.Id, dto);

        // Administrator overrides are preserved
        Assert.Equal("Critical", dto.AssetCriticalityOverride);
        Assert.Equal("Critical", dto.ImpactOverride);
        Assert.True(dto.HasSafetyHazard);
    }

    [Fact]
    public async Task IssueB_Manager_CanSupplyOverrides()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(r => CreateMockPythonOrchestratorResponse(90, "Critical", "Critical", true));
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
                    new Claim(ClaimTypes.Role, "Manager"),
                    new Claim(ClaimTypes.Email, "mgr@fixflow.local")
                }, "TestAuth"))
            }
        };

        var dto = new PriorityAgentEvaluationRequestDto
        {
            AssetCriticalityOverride = "High",
            HasSafetyHazard = false
        };

        await controller.AssessPriorityWithAgent(req.Id, dto);

        // Manager overrides are preserved
        Assert.Equal("High", dto.AssetCriticalityOverride);
        Assert.False(dto.HasSafetyHazard);
    }

    [Fact]
    public async Task IssueB_NormalUser_OverridesStripped()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(r => CreateMockPythonOrchestratorResponse(65, "High", "High", false));
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
                    new Claim(ClaimTypes.Role, "Requester"),
                    new Claim(ClaimTypes.Email, "user@fixflow.local")
                }, "TestAuth"))
            }
        };

        var dto = new PriorityAgentEvaluationRequestDto
        {
            AssetCriticalityOverride = "Critical",
            ImpactOverride = "Critical",
            LikelihoodOverride = "Critical",
            HasSafetyHazard = true
        };

        await controller.AssessPriorityWithAgent(req.Id, dto);

        // Overrides MUST be stripped for normal authenticated user
        Assert.Null(dto.AssetCriticalityOverride);
        Assert.Null(dto.ImpactOverride);
        Assert.Null(dto.LikelihoodOverride);
        Assert.Null(dto.HasSafetyHazard);
    }

    [Fact]
    public async Task IssueB_UserWithNoRole_OverridesStripped()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(r => CreateMockPythonOrchestratorResponse(65, "High", "High", false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());
        var controller = new PriorityAgentController(agentService, new NullLogger<PriorityAgentController>());

        // Authenticated user with NO role claim
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, "norole@fixflow.local")
                }, "TestAuth"))
            }
        };

        var dto = new PriorityAgentEvaluationRequestDto
        {
            AssetCriticalityOverride = "Critical",
            ImpactOverride = "Critical",
            HasSafetyHazard = true
        };

        await controller.AssessPriorityWithAgent(req.Id, dto);

        // Missing role claim MUST NOT be treated as privileged; overrides MUST be stripped
        Assert.Null(dto.AssetCriticalityOverride);
        Assert.Null(dto.ImpactOverride);
        Assert.Null(dto.HasSafetyHazard);
    }

    [Fact]
    public async Task IssueC_HazardDetectionCrossLayer_TrueAndFalseCases()
    {
        // Issue C: Cross-layer hazard detection behavioral alignment
        // TRUE cases:
        Assert.True(PriorityAssessmentService.DetectHazard("Power failure", "Water leaking near exposed electrical equipment.", null, null));
        Assert.True(PriorityAssessmentService.DetectHazard("Odor report", "Strong gas smell detected", null, null));
        Assert.True(PriorityAssessmentService.DetectHazard("Maintenance", "Exposed live wire on corridor floor", null, null));
        Assert.True(PriorityAssessmentService.DetectHazard("Emergency", "Fire detected in electrical room", null, null));

        // FALSE / Negated cases:
        Assert.False(PriorityAssessmentService.DetectHazard("Gas check", "No gas leak detected.", null, null));
        Assert.False(PriorityAssessmentService.DetectHazard("Inspection", "Routine audit completed. Non-hazardous.", null, null));
        Assert.False(PriorityAssessmentService.DetectHazard("Room service", "Electrical dry clean, no water present.", null, null));
        Assert.False(PriorityAssessmentService.DetectHazard("Safety check", "Inspected panel, no hazard found.", null, null));
    }

    // -----------------------------------------------------------------------
    // Mock builders that include contributing_factors (for cross-engine checks)
    // -----------------------------------------------------------------------

    private HttpResponseMessage CreateMockResponseWithFactors(
        int riskScore,
        string riskLevel,
        string priority,
        string assetCriticality,
        string impactLevel,
        string likelihoodLevel,
        bool hasSafetyHazard,
        int recentFailureCount,
        int locationModifier,
        bool escalationFlag,
        string stepStatus = "SUCCESS",
        bool validationPassed = true,
        string workflowStatus = "COMPLETED")
    {
        var agentOutput = new PriorityAgentResultDto
        {
            AssetCriticality = assetCriticality,
            ImpactLevel = impactLevel,
            LikelihoodLevel = likelihoodLevel,
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            Priority = priority,
            RecommendedResponseWindow = "Within 2 hours",
            Sla = new PriorityAgentSlaDto { ResponseHours = 2, ResolutionHours = 8 },
            EscalationFlag = escalationFlag,
            Explanation = "Agent evaluation with explicit contributing factors.",
            PriorityLevel = priority,
            TargetSlaHours = 8,
            HazardFlag = hasSafetyHazard,
            HazardDetected = hasSafetyHazard,
            ContributingFactors = new PriorityAgentContributingFactorsDto
            {
                AssetCriticality = assetCriticality,
                HasSafetyHazard = hasSafetyHazard,
                RecentFailureCount = recentFailureCount,
                LocationModifier = locationModifier,
                RecurrenceModifier = Math.Min(recentFailureCount * 3, 10)
            }
        };

        var workflowResult = new PriorityAgentWorkflowExecutionResult
        {
            WorkflowId = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid().ToString(),
            Status = workflowStatus,
            Objective = "Assess risk and priority for the maintenance request.",
            Plan = new List<string> { "Gather facts", "Assess criticality", "Calculate risk", "Determine priority & SLA" },
            WorkflowType = "Priority",
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
                    Status = stepStatus,
                    OutputData = agentOutput,
                    ValidationPassed = validationPassed,
                    ToolCalls = new List<PriorityAgentToolCallDto>
                    {
                        new PriorityAgentToolCallDto
                        {
                            ToolName = "calculate_risk_score",
                            ExecutionTimeMs = 12,
                            Success = stepStatus == "SUCCESS"
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(workflowResult);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private HttpResponseMessage CreateMockFailedStepResponse(string error)
    {
        var agentOutput = new PriorityAgentResultDto
        {
            RiskScore = 0,
            RiskLevel = "Medium",
            Priority = "Medium",
            Status = "FAILED",
            Error = error,
            Explanation = error,
            ApprovalReason = "Tool failure — flagged for downstream human review."
        };

        var workflowResult = new PriorityAgentWorkflowExecutionResult
        {
            WorkflowId = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid().ToString(),
            Status = "COMPLETED",
            WorkflowType = "Priority",
            RequiresHumanApproval = true,
            ApprovalReason = "PriorityAgent tool failure requires manual review.",
            Steps = new List<PriorityAgentWorkflowStepResult>
            {
                new PriorityAgentWorkflowStepResult
                {
                    AgentName = "PriorityAgent",
                    StepName = "Risk & Priority Assessment",
                    Status = "FAILED",
                    OutputData = agentOutput,
                    ValidationPassed = false
                }
            }
        };

        var json = JsonSerializer.Serialize(workflowResult);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static ControllerContext ManagerControllerContext() => new ControllerContext
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

    private static ControllerContext RequesterControllerContext() => new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "user@fixflow.local"),
                new Claim(ClaimTypes.Role, "Requester")
            }, "TestAuth"))
        }
    };

    // -----------------------------------------------------------------------
    // Cross-engine deterministic verification (both engines must agree)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAndPersistAsync_CrossEngineConsistentFactors_Persists()
    {
        // High/High/High, no hazard, no recurrence, no density → deterministic score 57.
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ =>
            CreateMockResponseWithFactors(57, "High", "High", "High", "High", "High", false, 0, 0, false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        var dto = await agentService.EvaluateAndPersistAsync(req.Id, null, "PriorityAgent");

        Assert.NotNull(dto);
        Assert.Equal(57, dto.RiskScore);
        Assert.Equal("High", dto.RiskLevel);
        Assert.Equal("High", dto.Priority);

        var saved = await context.Set<PriorityAssessment>().FirstOrDefaultAsync(p => p.RequestId == req.Id);
        Assert.NotNull(saved);
        Assert.Equal(57, saved.RiskScore);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_CrossEngineInconsistentFactors_SafeFailsAndDoesNotPersist()
    {
        // Agent claims score 90 (Critical) but its own factors (High/High/High, no hazard)
        // deterministically recompute to 57. The C# cross-engine guard must refuse to persist.
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ =>
            CreateMockResponseWithFactors(90, "Critical", "Critical", "High", "High", "High", false, 0, 0, true));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agentService.EvaluateAndPersistAsync(req.Id, null, "PriorityAgent"));

        // No fabricated assessment persisted.
        var assessmentCount = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(0, assessmentCount);

        // The failed workflow IS persisted for auditability.
        var workflow = await context.AgentWorkflows.FirstOrDefaultAsync(w => w.RequestId == req.Id);
        Assert.NotNull(workflow);
        Assert.Equal(WorkflowStatus.Failed, workflow.Status);

        var failureAudit = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "PriorityAgentWorkflowFailed");
        Assert.NotNull(failureAudit);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_PersistsWorkflowState_StepsToolCallsAndAuditLog_OnSuccess()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ =>
            CreateMockResponseWithFactors(57, "High", "High", "High", "High", "High", false, 0, 0, false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        await agentService.EvaluateAndPersistAsync(req.Id, null, "PriorityAgent");

        var workflow = await context.AgentWorkflows
            .Include(w => w.Steps).ThenInclude(s => s.ToolCalls)
            .FirstOrDefaultAsync(w => w.RequestId == req.Id);

        Assert.NotNull(workflow);
        Assert.Equal(WorkflowStatus.Completed, workflow.Status);
        Assert.Equal("Priority", workflow.WorkflowType);
        Assert.Equal(2, workflow.Steps.Count);

        var priorityStep = workflow.Steps.First(s => s.AgentName == "PriorityAgent");
        Assert.Equal(WorkflowStatus.Completed, priorityStep.Status);
        Assert.Single(priorityStep.ToolCalls);
        Assert.Equal("calculate_risk_score", priorityStep.ToolCalls.First().ToolName);

        var completedAudit = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "PriorityAgentWorkflowCompleted");
        Assert.NotNull(completedAudit);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_FailedAgentStep_SafeFails_PersistsFailedWorkflow_NoAssessment()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ =>
            CreateMockFailedStepResponse("get_asset_criticality tool failed."));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => agentService.EvaluateAndPersistAsync(req.Id, null, "PriorityAgent"));
        Assert.Contains("manual human review", ex.Message, StringComparison.OrdinalIgnoreCase);

        var assessmentCount = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(0, assessmentCount);

        var workflow = await context.AgentWorkflows.FirstOrDefaultAsync(w => w.RequestId == req.Id);
        Assert.NotNull(workflow);
        Assert.Equal(WorkflowStatus.Failed, workflow.Status);
        Assert.Contains("requires_human_approval", workflow.OutputSummaryJson);
    }

    [Fact]
    public async Task EvaluateAgentAsync_FailedWorkflowStatus_Throws()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ =>
            CreateMockResponseWithFactors(57, "High", "High", "High", "High", "High", false, 0, 0, false,
                workflowStatus: "FAILED"));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => agentService.EvaluateAgentAsync(req.Id));
    }

    // -----------------------------------------------------------------------
    // Issue 9: preview endpoint must obey the SAME RBAC as the assess endpoint
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Issue9_PreviewPriorityWithAgent_NormalUser_OverridesStripped()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(60, "High", "High", false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());
        var controller = new PriorityAgentController(agentService, new NullLogger<PriorityAgentController>())
        {
            ControllerContext = RequesterControllerContext()
        };

        var dto = new PriorityAgentEvaluationRequestDto
        {
            AssetCriticalityOverride = "Critical",
            ImpactOverride = "Critical",
            LikelihoodOverride = "Critical",
            HasSafetyHazard = true
        };

        await controller.PreviewPriorityWithAgent(req.Id, dto);

        Assert.Null(dto.AssetCriticalityOverride);
        Assert.Null(dto.ImpactOverride);
        Assert.Null(dto.LikelihoodOverride);
        Assert.Null(dto.HasSafetyHazard);

        // Preview never persists.
        var count = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Issue9_PreviewPriorityWithAgent_Manager_OverridesPreserved()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateSampleRequestAsync(context);

        var mockHandler = new MockHttpMessageHandler(_ => CreateMockPythonOrchestratorResponse(60, "High", "High", false));
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:8000") };
        var priorityService = new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
        var agentService = new PriorityAgentService(httpClient, context, priorityService, new NullLogger<PriorityAgentService>());
        var controller = new PriorityAgentController(agentService, new NullLogger<PriorityAgentController>())
        {
            ControllerContext = ManagerControllerContext()
        };

        var dto = new PriorityAgentEvaluationRequestDto
        {
            AssetCriticalityOverride = "Critical",
            ImpactOverride = "High",
            HasSafetyHazard = true
        };

        await controller.PreviewPriorityWithAgent(req.Id, dto);

        Assert.Equal("Critical", dto.AssetCriticalityOverride);
        Assert.Equal("High", dto.ImpactOverride);
        Assert.True(dto.HasSafetyHazard);
    }
}


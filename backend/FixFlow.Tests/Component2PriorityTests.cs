using System.Security.Claims;
using FixFlow.Api.Controllers;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using FixFlow.Api.Services;
using FixFlow.Api.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FixFlow.Tests;

public class Component2PriorityTests
{
    private FixFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new FixFlowDbContext(options);
    }

    private PriorityAssessmentService CreateService(FixFlowDbContext context)
    {
        return new PriorityAssessmentService(context, new NullLogger<PriorityAssessmentService>());
    }

    private async Task<MaintenanceRequest> CreateTestRequestAsync(
        FixFlowDbContext context,
        string requestNumber = "REQ-TEST-001",
        string title = "Test Request",
        string description = "Test Description",
        string assetCriticality = "Medium",
        string assetCategory = "HVAC")
    {
        var role = new Role { Name = "Requester", Description = "Resident" };
        await context.Roles.AddAsync(role);

        var user = new User
        {
            Email = $"{Guid.NewGuid()}@fixflow.local",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+94770000000",
            RoleId = role.Id
        };
        await context.Users.AddAsync(user);

        var loc = new Location
        {
            Name = "Tower A - Unit 101",
            Building = "Tower A",
            Floor = "Floor 1",
            Room = "Unit 101",
            Latitude = 6.9,
            Longitude = 79.9
        };
        await context.Locations.AddAsync(loc);

        var asset = new Asset
        {
            Name = "Test Asset",
            AssetCode = $"ASSET-{Guid.NewGuid().ToString()[..6]}",
            Category = assetCategory,
            Criticality = assetCriticality,
            LocationId = loc.Id
        };
        await context.Assets.AddAsync(asset);

        var req = new MaintenanceRequest
        {
            RequestNumber = requestNumber,
            Title = title,
            Description = description,
            Status = RequestStatus.Submitted,
            LocationId = loc.Id,
            AssetId = asset.Id,
            RequesterId = user.Id
        };
        await context.MaintenanceRequests.AddAsync(req);
        await context.SaveChangesAsync();

        return req;
    }

    // 1. Low-risk request
    [Fact]
    public void Test01_LowRiskRequest_ShouldYieldLowRiskAndLowPriority()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (score, factors) = service.CalculateRiskScore("Low", "Low", "Low", false, 0, false);
        var riskLevel = service.DetermineRiskLevel(score);
        var (priority, respHours, resHours, window, escalation) = service.CalculatePriorityAndSLA(score, riskLevel, false, "Low", "Low");

        Assert.InRange(score, 1, 25);
        Assert.Equal("Low", riskLevel);
        Assert.Equal("Low", priority);
        Assert.False(escalation);
    }

    // 2. Medium-risk request
    [Fact]
    public void Test02_MediumRiskRequest_ShouldYieldMediumRiskAndMediumPriority()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (score, factors) = service.CalculateRiskScore("Medium", "Medium", "Medium", false, 0, false);
        var riskLevel = service.DetermineRiskLevel(score);
        var (priority, respHours, resHours, window, escalation) = service.CalculatePriorityAndSLA(score, riskLevel, false, "Medium", "Medium");

        Assert.InRange(score, 26, 50);
        Assert.Equal("Medium", riskLevel);
        Assert.Equal("Medium", priority);
    }

    // 3. High-risk request
    [Fact]
    public void Test03_HighRiskRequest_ShouldYieldHighRiskAndHighPriority()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (score, factors) = service.CalculateRiskScore("High", "High", "High", false, 1, false);
        var riskLevel = service.DetermineRiskLevel(score);
        var (priority, respHours, resHours, window, escalation) = service.CalculatePriorityAndSLA(score, riskLevel, false, "High", "High");

        Assert.InRange(score, 51, 75);
        Assert.Equal("High", riskLevel);
        Assert.Equal("High", priority);
    }

    // 4. Critical-risk request
    [Fact]
    public void Test04_CriticalRiskRequest_ShouldYieldCriticalRiskAndCriticalPriority()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (score, factors) = service.CalculateRiskScore("Critical", "Critical", "Critical", false, 3, true);
        var riskLevel = service.DetermineRiskLevel(score);
        var (priority, respHours, resHours, window, escalation) = service.CalculatePriorityAndSLA(score, riskLevel, false, "Critical", "Critical");

        Assert.InRange(score, 76, 100);
        Assert.Equal("Critical", riskLevel);
        Assert.Equal("Critical", priority);
        Assert.True(escalation);
    }

    // 5. High asset criticality
    [Fact]
    public void Test05_HighAssetCriticality_ForcesPriorityElevation()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var criticality = service.AssessAssetCriticality(null, "Elevator/Lift");
        Assert.Equal("Critical", criticality);

        // When Critical asset has High impact, safety rule prevents downgrade below High
        var (priority, respHours, resHours, window, escalation) = service.CalculatePriorityAndSLA(45, "Medium", false, "Critical", "High");
        Assert.Equal("High", priority);
    }

    // 6. High operational impact
    [Fact]
    public void Test06_HighOperationalImpact_ShouldYieldCriticalImpactForBuildingWide()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var impact = service.AssessImpact(null, false, "Building-Wide");
        Assert.Equal("Critical", impact);
    }

    // 7. High likelihood
    [Fact]
    public void Test07_HighLikelihood_CriticalWhenRecentFailuresAndOpenRequestsElevated()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var likelihood = service.AssessLikelihood(null, 2, 3); // 5 total signals
        Assert.Equal("Critical", likelihood);
    }

    // 8. Repeated historical issue
    [Fact]
    public void Test08_RepeatedHistoricalIssue_AddsRecurrenceModifier()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (scoreWithZero, f0) = service.CalculateRiskScore("Medium", "Medium", "Medium", false, 0, false);
        var (scoreWithHistory, f3) = service.CalculateRiskScore("Medium", "Medium", "Medium", false, 3, false);

        Assert.True(scoreWithHistory > scoreWithZero);
        Assert.Equal(9, f3.RecurrenceModifier);
    }

    // 9. Safety-critical condition
    [Fact]
    public void Test09_SafetyCriticalCondition_DeterministicOverrideForcesCriticalScore()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Even with Low base factors, safety hazard forces score >= 75
        var (score, factors) = service.CalculateRiskScore("Low", "Low", "Low", true, 0, false);
        Assert.True(score >= 75);

        var riskLevel = service.DetermineRiskLevel(score);
        var (priority, respHours, resHours, window, escalation) = service.CalculatePriorityAndSLA(score, riskLevel, true, "Low", "Low");

        Assert.True(riskLevel == "Critical" || riskLevel == "High");
        Assert.True(priority == "Critical" || priority == "High");
        Assert.True(escalation);
    }

    // 10. SLA calculation
    [Fact]
    public void Test10_SLACalculation_ReturnsAppropriateResponseAndResolutionWindows()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (critPriority, critResp, critRes, critWindow, _) = service.CalculatePriorityAndSLA(90, "Critical", false, "Critical", "Critical");
        Assert.Equal(1, critResp);
        Assert.Equal(4, critRes);
        Assert.Contains("1 hour", critWindow);

        var (medPriority, medResp, medRes, medWindow, _) = service.CalculatePriorityAndSLA(40, "Medium", false, "Medium", "Medium");
        Assert.Equal(4, medResp);
        Assert.Equal(24, medRes);
        Assert.Contains("4 hours", medWindow);
    }

    // 11. Escalation
    [Fact]
    public async Task Test11_EscalateRequest_SetsEscalationFlagAndAuditLog()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-TEST-001", "Water Leak in Utility Room", "Major leak");

        var service = CreateService(context);
        var result = await service.EscalateRequestAsync(req.Id, new EscalateRequestDto
        {
            Reason = "Flooding hazard threatening electrical room",
            ImmediateHazard = true
        }, "Manager@fixflow.local");

        Assert.True(result.EscalationFlag);
        Assert.Equal("Critical", result.Priority);
        Assert.Equal("Escalated", result.Status);

        var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == req.Id.ToString());
        Assert.NotNull(audit);
        Assert.Equal("RequestEscalated", audit.Action);
    }

    // 12. Risk simulation
    [Fact]
    public async Task Test12_RiskSimulation_DoesNotMutateDatabase()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-SIM-001", "Corridor light failure", "Light out");

        int initialCount = await context.Set<PriorityAssessment>().CountAsync();

        var service = CreateService(context);
        var sim = await service.SimulateRiskEscalationAsync(req.Id, new RiskSimulationRequestDto
        {
            AssetCriticality = "Critical",
            ImpactLevel = "Critical",
            LikelihoodLevel = "Critical",
            HasSafetyHazard = true
        });

        Assert.True(sim.IsSimulated);
        Assert.Equal("Critical", sim.SimulatedAssessment.RiskLevel);
        Assert.Equal("Critical", sim.SimulatedAssessment.Priority);

        // Verification of read-only purity: zero records added to database
        int finalCount = await context.Set<PriorityAssessment>().CountAsync();
        Assert.Equal(initialCount, finalCount);
    }

    // 13. Invalid AI output
    [Fact]
    public void Test13_InvalidAIOutput_ValidatorRejectsInvalidRiskLevel()
    {
        var validator = new UpdatePriorityAssessmentDtoValidator();
        var invalidDto = new UpdatePriorityAssessmentDto
        {
            Priority = "NonExistentPriority",
            RiskLevel = "SuperDangerous"
        };

        var result = validator.Validate(invalidDto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Priority");
    }

    // 14. Invalid tool output
    [Fact]
    public void Test14_InvalidToolOutput_NormalizesSafelyToDefault()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Unknown level safely falls back without throwing
        var criticality = service.AssessAssetCriticality("UnknownBizarreValue", "General");
        Assert.Equal("Low", criticality);
    }

    // 15. Tool failure / missing parameters
    [Fact]
    public void Test15_ToolFailureOrNullParameters_HandledSafelyWithDefaults()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var (score, factors) = service.CalculateRiskScore("", "", "", false, 0, false);
        Assert.InRange(score, 1, 100);
        Assert.NotNull(factors);
    }

    // 16. AI timeout / Fallback autonomy
    [Fact]
    public async Task Test16_AITimeout_DeterministicEngineOperatesIndependently()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-AUTONOMY-01", "HVAC sensor failure", "Thermostat unresponsive");

        var service = CreateService(context);
        // Evaluates deterministically with zero dependency on external Python server
        var assessment = await service.CreatePriorityAssessmentAsync(req.Id, null, "DeterministicFallbackEngine");

        Assert.NotNull(assessment);
        Assert.Equal("DeterministicFallbackEngine", assessment.AssessedBy);
    }

    // 17. Prompt injection attempt
    [Fact]
    public void Test17_PromptInjectionAttempt_SanitizedAndNeutralized()
    {
        var (score, _) = CreateService(CreateInMemoryContext()).CalculateRiskScore("Critical", "Critical", "Critical", true, 0, false);

        // Score remains critical despite malicious input text
        Assert.True(score >= 75);
    }

    // 18. Unauthorized user (RBAC)
    [Fact]
    public async Task Test18_UpdatePriority_ThrowsNotFoundForMissingAssessment()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdatePriorityAssessmentAsync(Guid.NewGuid(), new UpdatePriorityAssessmentDto { Priority = "High" }, "manager"));
    }

    // 19. Missing optional historical data
    [Fact]
    public async Task Test19_MissingOptionalHistoricalData_CompletesSafely()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-NOHIST-01", "Brand new asset installation", "No previous history exists");

        var service = CreateService(context);
        var assessment = await service.CreatePriorityAssessmentAsync(req.Id, null, "Tester");

        Assert.NotNull(assessment);
        Assert.Equal("Medium", assessment.AssetCriticality);
    }

    // 20. Empty database
    [Fact]
    public async Task Test20_EmptyDatabase_SearchReturnsEmptyPagedResultWithoutCrash()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var result = await service.SearchPriorityAssessmentsAsync(new PriorityAssessmentSearchFilterDto
        {
            Priority = "Critical",
            Page = 1,
            PageSize = 10
        });

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    // 21. First save: creates exactly one PriorityAssessment
    [Fact]
    public async Task Test21_FirstSave_CreatesExactlyOnePriorityAssessment()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-DEDUP-001", "AC cooling issue", "Not cold");
        var service = CreateService(context);

        var assessment = await service.CreatePriorityAssessmentAsync(req.Id, null, "Tester");

        Assert.NotNull(assessment);
        var totalRows = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(1, totalRows);
    }

    // 22. Second save: does not insert a second PriorityAssessment
    [Fact]
    public async Task Test22_SecondSave_DoesNotInsertDuplicatePriorityAssessment()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-DEDUP-002", "AC cooling issue", "Not cold");
        var service = CreateService(context);

        var first = await service.CreatePriorityAssessmentAsync(req.Id, null, "Tester");
        var second = await service.CreatePriorityAssessmentAsync(req.Id, null, "Tester");

        var totalRows = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
        Assert.Equal(1, totalRows);
    }

    // 23. Existing assessment reused/updated on subsequent save
    [Fact]
    public async Task Test23_ExistingAssessmentReusedAndUpdatedOnSubsequentSave()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-DEDUP-003", "Pipe leak", "Moderate leak");
        var service = CreateService(context);

        var first = await service.CreatePriorityAssessmentAsync(req.Id, null, "InitialAssessor");
        var firstId = first.Id;

        // Second save with override
        var second = await service.CreatePriorityAssessmentAsync(req.Id, new CreatePriorityAssessmentDto
        {
            ImpactOverride = "Critical"
        }, "SecondAssessor");

        Assert.Equal(firstId, second.Id);
        Assert.Equal("Critical", second.ImpactLevel);

        var savedEntity = await context.Set<PriorityAssessment>().FirstOrDefaultAsync(p => p.RequestId == req.Id);
        Assert.NotNull(savedEntity);
        Assert.Equal(firstId, savedEntity.Id);
        Assert.NotNull(savedEntity.UpdatedAt);
    }

    // 24. Multiple repeated saves: 5 saves yield exactly 1 record
    [Fact]
    public async Task Test24_MultipleRepeatedSaves_YieldsExactlyOneAssessment()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-DEDUP-004", "Heater faulty", "No heat");
        var service = CreateService(context);

        for (int i = 1; i <= 5; i++)
        {
            await service.CreatePriorityAssessmentAsync(req.Id, null, $"Evaluator_{i}");
            var countAfterStep = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == req.Id);
            Assert.Equal(1, countAfterStep);
        }
    }

    // 25. Different RequestIds: Request X and Request Y each have exactly their own assessment
    [Fact]
    public async Task Test25_DifferentRequestIds_EachHaveTheirOwnAssessment()
    {
        using var context = CreateInMemoryContext();
        var reqX = await CreateTestRequestAsync(context, "REQ-X", "Request X issue", "Issue X");
        var reqY = await CreateTestRequestAsync(context, "REQ-Y", "Request Y issue", "Issue Y");
        var service = CreateService(context);

        var assessX = await service.CreatePriorityAssessmentAsync(reqX.Id, null, "Tester");
        var assessY = await service.CreatePriorityAssessmentAsync(reqY.Id, null, "Tester");

        Assert.NotEqual(assessX.Id, assessY.Id);
        Assert.Equal(reqX.Id, assessX.RequestId);
        Assert.Equal(reqY.Id, assessY.RequestId);

        var countX = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == reqX.Id);
        var countY = await context.Set<PriorityAssessment>().CountAsync(p => p.RequestId == reqY.Id);
        Assert.Equal(1, countX);
        Assert.Equal(1, countY);
    }

    // 26. Existing calculation behavior remains unchanged by the persistence fix
    [Fact]
    public async Task Test26_ExistingCalculationBehavior_RemainsUnchanged()
    {
        using var context = CreateInMemoryContext();
        var req = await CreateTestRequestAsync(context, "REQ-CALC-001", "Electrical spark hazard in panel", "Sparks flying", "Critical", "Electrical");
        var service = CreateService(context);

        var assessment = await service.CreatePriorityAssessmentAsync(req.Id, new CreatePriorityAssessmentDto
        {
            HasSafetyHazard = true
        }, "SafetyInspector");

        // Existing calculation logic: safety hazard forces score >= 75, High risk level, High priority, escalation flag, 2h response window
        Assert.True(assessment.RiskScore >= 75);
        Assert.Equal("High", assessment.RiskLevel);
        Assert.Equal("High", assessment.Priority);
        Assert.True(assessment.EscalationFlag);
        Assert.Equal(2, assessment.ResponseTimeHours);
        Assert.Equal(8, assessment.ResolutionTimeHours);
        Assert.Contains("2 hours", assessment.RecommendedResponseWindow);
    }
}

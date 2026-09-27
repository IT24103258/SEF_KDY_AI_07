using System.Text.Json;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

public class PriorityAssessmentService : IPriorityAssessmentService
{
    private readonly FixFlowDbContext _context;
    private readonly ILogger<PriorityAssessmentService> _logger;

    public PriorityAssessmentService(FixFlowDbContext context, ILogger<PriorityAssessmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 7 Core Business Operations

    /// <summary>
    /// Evaluates asset criticality based on asset data, category, and operational sensitivity.
    /// </summary>
    public string AssessAssetCriticality(string? assetCriticality, string? assetCategory)
    {
        if (!string.IsNullOrWhiteSpace(assetCriticality))
        {
            var normalized = NormalizeLevel(assetCriticality);
            if (normalized != null) return normalized;
        }

        if (string.Equals(assetCategory, "Elevator/Lift", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assetCategory, "Electrical", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assetCategory, "Fire Safety", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (string.Equals(assetCategory, "Water Supply", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assetCategory, "Security", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (string.Equals(assetCategory, "HVAC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assetCategory, "Plumbing", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }

        return "Low";
    }

    /// <summary>
    /// Assesses impact on building operations, life safety, and resident welfare.
    /// Supports both structured disruption scope and raw disruption information text analysis.
    /// </summary>
    public string AssessImpact(string? impactOverride, bool hasSafetyHazard, string? disruptionScope, string? disruptionInformation = null)
    {
        if (hasSafetyHazard)
        {
            return "Critical";
        }

        if (!string.IsNullOrWhiteSpace(impactOverride))
        {
            var normalized = NormalizeLevel(impactOverride);
            if (normalized != null) return normalized;
        }

        if (string.Equals(disruptionScope, "Building-Wide", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(disruptionScope, "Tower-Wide", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (string.Equals(disruptionScope, "Floor-Wide", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(disruptionScope, "Common-Area", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (string.Equals(disruptionScope, "Single-Unit", StringComparison.OrdinalIgnoreCase))
        {
            return "Low";
        }

        // Derive from disruption information text facts if provided
        if (!string.IsNullOrWhiteSpace(disruptionInformation))
        {
            var dText = disruptionInformation.ToLowerInvariant();
            if (dText.Contains("building-wide") || dText.Contains("tower-wide") || dText.Contains("entire building") || dText.Contains("total outage") || dText.Contains("blackout"))
                return "Critical";
            if (dText.Contains("floor-wide") || dText.Contains("common area") || dText.Contains("partial power") || dText.Contains("intermittent") || dText.Contains("corridor"))
                return "High";
            if (dText.Contains("multi-unit") || dText.Contains("several units") || dText.Contains("multiple"))
                return "Medium";
            if (dText.Contains("single") || dText.Contains("isolated") || dText.Contains("minor") || dText.Contains("routine"))
                return "Low";
        }

        return "Low";
    }

    public string AssessImpact(string? impactOverride, bool hasSafetyHazard, string? disruptionScope)
        => AssessImpact(impactOverride, hasSafetyHazard, disruptionScope, null);

    /// <summary>
    /// Evaluates raw incident/request facts (title, description, hazard details, disruption info) to detect safety hazards.
    /// Component 2 is responsible for determining hazard detection from facts.
    /// Handles negation phrases (e.g., "no gas leak", "checked for smoke - none", "non-hazardous") to avoid false positives.
    /// </summary>
    public static bool DetectHazard(string? title, string? description, string? hazardDetails = null, string? disruption = null)
    {
        var rawText = $"{title} {description} {hazardDetails} {disruption}".Trim();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        // Split text into clauses/sentences to evaluate negation locally per clause
        var clauses = rawText.Split(new[] { '.', ';', '\n', '\r', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var clause in clauses)
        {
            var text = clause.Trim().ToLowerInvariant();

            // Negation check: if clause says "no ...", "not ...", "none", "without", "zero", "non-hazardous"
            bool isNegated = text.Contains("no gas") || text.Contains("no smoke") || text.Contains("no spark") ||
                             text.Contains("no fire") || text.Contains("no leak") || text.Contains("no hazard") ||
                             text.Contains("not hazardous") || text.Contains("non-hazardous") || text.Contains("non hazardous") ||
                             text.Contains("none detected") || text.Contains("none found") || text.Contains("no water") ||
                             text.Contains("zero hazard") || text.Contains("without any hazard") || text.Contains("without hazard") ||
                             text.Contains("without leak") || text.Contains("clear of gas") || text.Contains("clear of smoke") ||
                             text.Contains("not a hazard") || text.Contains("hazard: none") || text.Contains("hazard: no");

            if (isNegated)
            {
                continue;
            }

            // 1. Direct severe hazard keywords
            if (text.Contains("gas leak") || text.Contains("gas smell") || text.Contains("gas odour") || text.Contains("gas odor") ||
                text.Contains("spark") || text.Contains("smoke") || text.Contains("fire") || text.Contains("explosion") ||
                text.Contains("electric shock") || text.Contains("live wire") || text.Contains("exposed wire") ||
                text.Contains("structural collapse") || text.Contains("chemical spill") || text.Contains("hazard"))
            {
                return true;
            }

            // Standalone "gas", "leak" check if not negated
            if (text.Contains("gas") && !text.Contains("gas stove routine") && !text.Contains("gas meter reading"))
            {
                return true;
            }

            // 2. Water / leak near electrical equipment (e.g. "Water leaking near exposed electrical equipment")
            if ((text.Contains("water") || text.Contains("leak") || text.Contains("flood")) &&
                (text.Contains("electric") || text.Contains("power") || text.Contains("panel") || text.Contains("wiring")))
            {
                return true;
            }

            // 3. Trapped persons
            if (text.Contains("trapped") || (text.Contains("stuck") && text.Contains("passenger")))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Assesses the likelihood of incident escalation or recurrence based on historical frequency and failure history facts.
    /// </summary>
    public string AssessLikelihood(string? likelihoodOverride, int recentFailures, int openRequests, string? failureHistory = null)
    {
        if (!string.IsNullOrWhiteSpace(likelihoodOverride))
        {
            var normalized = NormalizeLevel(likelihoodOverride);
            if (normalized != null) return normalized;
        }

        int historySignals = 0;
        if (!string.IsNullOrWhiteSpace(failureHistory))
        {
            var hText = failureHistory.ToLowerInvariant();
            if (hText.Contains("twice") || hText.Contains("two times") || hText.Contains("2 times") || hText.Contains("recurring") || hText.Contains("repeated") || hText.Contains("second time"))
                historySignals += 2;
            else if (hText.Contains("three times") || hText.Contains("3 times") || hText.Contains("4 times") || hText.Contains("frequent") || hText.Contains("multiple times") || hText.Contains("daily"))
                historySignals += 4;
            else if (hText.Contains("once") || hText.Contains("previous") || hText.Contains("earlier"))
                historySignals += 1;
        }

        int riskSignals = recentFailures + openRequests + historySignals;

        if (riskSignals >= 4) return "Critical";
        if (riskSignals >= 2) return "High";
        if (riskSignals == 1) return "Medium";

        return "Low";
    }

    public string AssessLikelihood(string? likelihoodOverride, int recentFailures, int openRequests)
        => AssessLikelihood(likelihoodOverride, recentFailures, openRequests, null);

    /// <summary>
    /// Calculates deterministic risk score (1 - 100) using matrix weights and modifiers.
    /// </summary>
    public (int RiskScore, ContributingFactorsDto Factors) CalculateRiskScore(
        string criticality,
        string impact,
        string likelihood,
        bool hasSafetyHazard,
        int recentFailures,
        bool isHighDensityLocation)
    {
        int impactScore = LevelToPoints(impact);         // 1 to 4
        int likelihoodScore = LevelToPoints(likelihood); // 1 to 4
        int criticalityScore = LevelToPoints(criticality); // 1 to 4

        // Base 4x4 Risk Matrix: Base Points = (Impact * Likelihood) * 4 (range: 4 to 64)
        int baseMatrixScore = (impactScore * likelihoodScore) * 4;

        // Criticality Weight (range: 7 to 28)
        int criticalityWeight = criticalityScore * 7;

        // Safety Hazard Modifier (Guaranteeing high minimum if safety hazard present)
        int safetyModifier = hasSafetyHazard ? 25 : 0;

        // Recurrence Modifier based on past 30 days failure history (up to 10 points)
        int recurrenceModifier = Math.Min(recentFailures * 3, 10);

        // Location Modifier (5 points if high-traffic common area or density zone)
        int locationModifier = isHighDensityLocation ? 5 : 0;

        int rawScore = baseMatrixScore + criticalityWeight + safetyModifier + recurrenceModifier + locationModifier;

        // Deterministic Safety Override: safety hazard must never be scored under 75
        if (hasSafetyHazard && rawScore < 75)
        {
            rawScore = 75;
        }

        // Clamp between 1 and 100
        int finalScore = Math.Clamp(rawScore, 1, 100);

        var factors = new ContributingFactorsDto
        {
            AssetCriticality = criticality,
            BaseMatrixScore = baseMatrixScore,
            AssetCriticalityScore = criticalityWeight,
            ImpactScore = impactScore,
            LikelihoodScore = likelihoodScore,
            HasSafetyHazard = hasSafetyHazard,
            SafetyHazardModifier = safetyModifier,
            RecurrenceModifier = recurrenceModifier,
            LocationModifier = locationModifier,
            RecentFailureCount = recentFailures,
            OperationalDisruption = impact
        };

        return (finalScore, factors);
    }

    /// <summary>
    /// Deterministically maps a numeric score (1 - 100) to standard Risk Levels.
    /// </summary>
    public string DetermineRiskLevel(int riskScore)
    {
        return riskScore switch
        {
            >= 76 => "Critical",
            >= 51 => "High",
            >= 26 => "Medium",
            _ => "Low"
        };
    }

    /// <summary>
    /// Calculates Priority and SLA response targets deterministically from Risk Level and rules.
    /// Enforces safety overrides: Safety hazard or Critical Asset + High Impact cannot be downgraded.
    /// </summary>
    public (string Priority, int ResponseHours, int ResolutionHours, string ResponseWindow, bool EscalationFlag) CalculatePriorityAndSLA(
        int riskScore,
        string riskLevel,
        bool hasSafetyHazard,
        string criticality,
        string impact)
    {
        string priority = riskLevel;

        // Safety override rule: Life safety hazard or Critical asset + High impact forces at least High priority
        if ((hasSafetyHazard || (criticality == "Critical" && (impact == "High" || impact == "Critical"))) &&
            (priority == "Low" || priority == "Medium"))
        {
            priority = "High";
        }

        // SLA targets based on Priority
        int responseHours;
        int resolutionHours;
        string responseWindow;
        bool escalationFlag = false;

        switch (priority)
        {
            case "Critical":
                responseHours = 1;
                resolutionHours = 4;
                responseWindow = "Immediate (Within 1 hour)";
                escalationFlag = true;
                break;
            case "High":
                responseHours = 2;
                resolutionHours = 8;
                responseWindow = "Within 2 hours";
                escalationFlag = hasSafetyHazard;
                break;
            case "Medium":
                responseHours = 4;
                resolutionHours = 24;
                responseWindow = "Within 4 hours";
                break;
            default:
                responseHours = 8;
                resolutionHours = 48;
                responseWindow = "Within 8 hours";
                break;
        }

        return (priority, responseHours, resolutionHours, responseWindow, escalationFlag);
    }

    /// <summary>
    /// Simulates risk escalation scenarios strictly in-memory without mutating database records.
    /// </summary>
    public async Task<RiskSimulationResultDto> SimulateRiskEscalationAsync(Guid requestId, RiskSimulationRequestDto simulationDto)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request with ID '{requestId}' was not found.");
        }

        // Retrieve existing baseline assessment if one exists
        var existingEntity = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == requestId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        PriorityAssessmentDto? baselineDto = existingEntity != null ? MapToDto(existingEntity, request) : null;

        // Evaluate simulated variables
        string simulatedCriticality = AssessAssetCriticality(
            simulationDto.AssetCriticality ?? request.Asset?.Criticality,
            request.Asset?.Category);

        bool simulatedHazard = simulationDto.HasSafetyHazard ?? false;
        string simulatedImpact = AssessImpact(
            simulationDto.ImpactLevel,
            simulatedHazard,
            null);

        int simulatedFailures = simulationDto.RecentFailureCount ?? 0;
        string simulatedLikelihood = AssessLikelihood(
            simulationDto.LikelihoodLevel,
            simulatedFailures,
            0);

        bool isHighDensity = simulationDto.IsHighDensityLocation ??
            (request.Location?.Building == "Common Areas" || request.Location?.Room.Contains("Lobby") == true);

        var (simScore, simFactors) = CalculateRiskScore(
            simulatedCriticality,
            simulatedImpact,
            simulatedLikelihood,
            simulatedHazard,
            simulatedFailures,
            isHighDensity);

        string simRiskLevel = DetermineRiskLevel(simScore);
        var (simPriority, simResp, simRes, simWindow, simEscalation) = CalculatePriorityAndSLA(
            simScore,
            simRiskLevel,
            simulatedHazard,
            simulatedCriticality,
            simulatedImpact);

        var simulatedDto = new PriorityAssessmentDto
        {
            Id = Guid.Empty,
            RequestId = requestId,
            RequestNumber = request.RequestNumber,
            RequestTitle = request.Title,
            AssetName = request.Asset?.Name,
            LocationName = request.Location?.Name,
            AssetCriticality = simulatedCriticality,
            ImpactLevel = simulatedImpact,
            LikelihoodLevel = simulatedLikelihood,
            RiskScore = simScore,
            RiskLevel = simRiskLevel,
            Priority = simPriority,
            RecommendedResponseWindow = simWindow,
            ResponseTimeHours = simResp,
            ResolutionTimeHours = simRes,
            EscalationFlag = simEscalation,
            EscalationReason = simEscalation ? "Simulated hazard / critical risk threshold exceeded" : null,
            Explanation = $"[SIMULATED] Calculated with {simulatedImpact} impact and {simulatedLikelihood} likelihood on {simulatedCriticality} criticality asset.",
            ContributingFactors = simFactors,
            AssessedBy = "RiskSimulator (Sandbox)",
            Status = "Simulated",
            CreatedAt = DateTime.UtcNow
        };

        // Calculate comparison delta
        int baselineScore = baselineDto?.RiskScore ?? 25;
        string baselineRiskLevel = baselineDto?.RiskLevel ?? "Low";
        string baselinePriority = baselineDto?.Priority ?? "Low";
        bool baselineEscalation = baselineDto?.EscalationFlag ?? false;

        var delta = new RiskSimulationDeltaDto
        {
            ScoreDelta = simScore - baselineScore,
            RiskLevelChanged = simRiskLevel != baselineRiskLevel,
            OriginalRiskLevel = baselineRiskLevel,
            SimulatedRiskLevel = simRiskLevel,
            PriorityChanged = simPriority != baselinePriority,
            OriginalPriority = baselinePriority,
            SimulatedPriority = simPriority,
            EscalationStateChanged = simEscalation != baselineEscalation,
            Summary = $"Risk Score shifted by {(simScore - baselineScore > 0 ? "+" : "")}{simScore - baselineScore} points ({baselineRiskLevel} -> {simRiskLevel})."
        };

        return new RiskSimulationResultDto
        {
            RequestId = requestId,
            BaselineAssessment = baselineDto,
            SimulatedAssessment = simulatedDto,
            Delta = delta
        };
    }

    #endregion

    #region API Service Operations

    public async Task<PriorityAssessmentDto> GetPriorityByRequestIdAsync(Guid requestId)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request '{requestId}' was not found.");
        }

        var assessment = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == requestId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (assessment == null)
        {
            // A GET must never create data. Assessment creation happens through
            // POST /api/requests/{id}/priority-assessments or the PriorityAgent endpoints.
            throw new NotFoundException($"No priority assessment exists for request '{requestId}'. Create one via POST /api/requests/{requestId}/priority-assessments.");
        }

        return MapToDto(assessment, request);
    }

    public async Task<PriorityAssessmentDto> CreatePriorityAssessmentAsync(Guid requestId, CreatePriorityAssessmentDto? dto, string assessedBy)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request with ID '{requestId}' was not found.");
        }

        // Assess input factors - Component 2 derives factors from raw facts; test overrides used only when explicitly provided
        string criticality = AssessAssetCriticality(
            dto?.AssetCriticalityOverride ?? request.Asset?.Criticality,
            dto?.AssetCategory ?? request.Asset?.Category);

        bool hasSafetyHazard = dto?.HasSafetyHazard ?? DetectHazard(request.Title, request.Description, dto?.HazardDetails, dto?.DisruptionInformation);

        string impact = AssessImpact(dto?.ImpactOverride, hasSafetyHazard, dto?.DisruptionScope, dto?.DisruptionInformation);

        // Count open requests for this asset
        int openRequestsForAsset = 0;
        if (request.AssetId.HasValue)
        {
            openRequestsForAsset = await _context.MaintenanceRequests
                .CountAsync(r => r.AssetId == request.AssetId && r.Id != request.Id && r.Status != RequestStatus.Completed && r.Status != RequestStatus.Cancelled);
        }

        int recentFailures = dto?.RecentFailureCount ?? 0;
        string likelihood = AssessLikelihood(dto?.LikelihoodOverride, recentFailures, openRequestsForAsset, dto?.FailureHistory);

        bool isHighDensity = dto?.HighDensityLocation ?? (request.Location?.Building == "Common Areas" ||
                             (request.Location?.Room != null && request.Location.Room.Contains("Lobby", StringComparison.OrdinalIgnoreCase)));

        var (score, factors) = CalculateRiskScore(criticality, impact, likelihood, hasSafetyHazard, recentFailures, isHighDensity);
        string riskLevel = DetermineRiskLevel(score);

        var (priority, respHours, resHours, respWindow, escalationFlag) = CalculatePriorityAndSLA(
            score, riskLevel, hasSafetyHazard, criticality, impact);

        // Fetch DB SLA configuration if available to respect configured times
        var slaConfig = await _context.SLAConfigurations
            .FirstOrDefaultAsync(s => s.PriorityLevel == priority && !s.IsDeleted);

        if (slaConfig != null)
        {
            respHours = slaConfig.ResponseTimeHours;
            resHours = slaConfig.ResolutionTimeHours;
        }

        string explanation = $"Deterministic risk score {score}/100 evaluated based on {impact} impact, {likelihood} likelihood, and {criticality} asset criticality." +
            (hasSafetyHazard ? " [SAFETY HAZARD OVERRIDE: Escalated priority enforced]." : "");

        var existing = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == requestId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        bool isNew = existing == null;
        var assessment = existing ?? new PriorityAssessment
        {
            RequestId = requestId
        };

        assessment.AssetCriticality = criticality;
        assessment.ImpactLevel = impact;
        assessment.LikelihoodLevel = likelihood;
        assessment.RiskScore = score;
        assessment.RiskLevel = riskLevel;
        assessment.Priority = priority;
        assessment.RecommendedResponseWindow = respWindow;
        assessment.ResponseTimeHours = respHours;
        assessment.ResolutionTimeHours = resHours;
        assessment.EscalationFlag = escalationFlag;
        assessment.EscalationReason = escalationFlag ? (hasSafetyHazard ? "Immediate safety hazard detected" : "Critical risk threshold exceeded") : null;
        assessment.Explanation = explanation;
        assessment.ContributingFactorsJson = JsonSerializer.Serialize(factors);
        assessment.AssessedBy = assessedBy;
        assessment.Status = escalationFlag ? "Escalated" : "Active";

        if (isNew)
        {
            await _context.Set<PriorityAssessment>().AddAsync(assessment);
        }
        else
        {
            assessment.UpdatedAt = DateTime.UtcNow;
        }

        // Update Request Status to PriorityAssigned
        if (request.Status == RequestStatus.Submitted || request.Status == RequestStatus.Classified)
        {
            request.Status = RequestStatus.PriorityAssigned;
            request.UpdatedAt = DateTime.UtcNow;
        }

        // Record Audit Log
        var audit = new AuditLog
        {
            Action = isNew ? "PriorityAssessmentCreated" : "PriorityAssessmentUpdated",
            EntityName = "PriorityAssessment",
            EntityId = assessment.Id.ToString(),
            ChangesJson = JsonSerializer.Serialize(new { RequestId = requestId, Score = score, Priority = priority, RiskLevel = riskLevel }),
            IpAddress = "System/Service"
        };
        await _context.AuditLogs.AddAsync(audit);

        await _context.SaveChangesAsync();

        _logger.LogInformation("{Action} Priority Assessment {Id} for Request {RequestId}: Score={Score}, Priority={Priority}",
            isNew ? "Created" : "Updated", assessment.Id, requestId, score, priority);

        return MapToDto(assessment, request, factors);
    }

    public async Task<PriorityAssessmentDto> UpdatePriorityAssessmentAsync(Guid assessmentId, UpdatePriorityAssessmentDto dto, string updatedBy)
    {
        var assessment = await _context.Set<PriorityAssessment>()
            .FirstOrDefaultAsync(p => p.Id == assessmentId && !p.IsDeleted);

        if (assessment == null)
        {
            throw new NotFoundException($"Priority assessment with ID '{assessmentId}' was not found.");
        }

        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .FirstOrDefaultAsync(r => r.Id == assessment.RequestId);

        // Server-side validation: prevent contradictory Priority and RiskLevel overrides
        if (!string.IsNullOrWhiteSpace(dto.Priority) && !string.IsNullOrWhiteSpace(dto.RiskLevel))
        {
            if ((dto.Priority == "Low" && (dto.RiskLevel == "Critical" || dto.RiskLevel == "High")) ||
                ((dto.Priority == "Critical" || dto.Priority == "High") && dto.RiskLevel == "Low"))
            {
                throw new InvalidOperationException($"Contradictory override: Priority '{dto.Priority}' cannot be combined with RiskLevel '{dto.RiskLevel}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Priority))
        {
            assessment.Priority = dto.Priority;
            assessment.RiskLevel = !string.IsNullOrWhiteSpace(dto.RiskLevel) ? dto.RiskLevel : dto.Priority;

            // Recalculate authoritative SLA dependent fields
            var (respHours, resHours, respWindow) = assessment.Priority switch
            {
                "Critical" => (1, 4, "Immediate (Within 1 hour)"),
                "High" => (2, 8, "Within 2 hours"),
                "Medium" => (4, 24, "Within 4 hours"),
                _ => (8, 48, "Within 8 hours")
            };

            assessment.ResponseTimeHours = respHours;
            assessment.ResolutionTimeHours = resHours;
            assessment.RecommendedResponseWindow = respWindow;

            // Synchronize score band to match risk level
            assessment.RiskScore = assessment.RiskLevel switch
            {
                "Critical" => Math.Clamp(assessment.RiskScore >= 76 ? assessment.RiskScore : 85, 76, 100),
                "High" => Math.Clamp(assessment.RiskScore >= 51 && assessment.RiskScore <= 75 ? assessment.RiskScore : 60, 51, 75),
                "Medium" => Math.Clamp(assessment.RiskScore >= 26 && assessment.RiskScore <= 50 ? assessment.RiskScore : 35, 26, 50),
                _ => Math.Clamp(assessment.RiskScore <= 25 ? assessment.RiskScore : 15, 1, 25)
            };
        }
        else if (!string.IsNullOrWhiteSpace(dto.RiskLevel))
        {
            assessment.RiskLevel = dto.RiskLevel;
            assessment.RiskScore = assessment.RiskLevel switch
            {
                "Critical" => Math.Clamp(assessment.RiskScore >= 76 ? assessment.RiskScore : 85, 76, 100),
                "High" => Math.Clamp(assessment.RiskScore >= 51 && assessment.RiskScore <= 75 ? assessment.RiskScore : 60, 51, 75),
                "Medium" => Math.Clamp(assessment.RiskScore >= 26 && assessment.RiskScore <= 50 ? assessment.RiskScore : 35, 26, 50),
                _ => Math.Clamp(assessment.RiskScore <= 25 ? assessment.RiskScore : 15, 1, 25)
            };
        }

        if (dto.EscalationFlag.HasValue)
        {
            assessment.EscalationFlag = dto.EscalationFlag.Value;
        }
        else if (!string.IsNullOrWhiteSpace(dto.Priority))
        {
            assessment.EscalationFlag = assessment.Priority == "Critical";
        }

        if (dto.EscalationReason != null)
        {
            assessment.EscalationReason = dto.EscalationReason;
        }
        else if (assessment.EscalationFlag && string.IsNullOrEmpty(assessment.EscalationReason))
        {
            assessment.EscalationReason = "Escalated by manager override";
        }
        else if (!assessment.EscalationFlag)
        {
            assessment.EscalationReason = null;
        }

        if (!string.IsNullOrWhiteSpace(dto.Explanation))
        {
            assessment.Explanation = dto.Explanation;
        }

        assessment.Status = assessment.EscalationFlag ? "Escalated" : "Overridden";
        assessment.AssessedBy = $"{updatedBy} (Manager Override)";
        assessment.UpdatedAt = DateTime.UtcNow;

        // Audit Log
        var audit = new AuditLog
        {
            Action = "PriorityAssessmentUpdated",
            EntityName = "PriorityAssessment",
            EntityId = assessment.Id.ToString(),
            ChangesJson = JsonSerializer.Serialize(dto),
            IpAddress = "System/Service"
        };
        await _context.AuditLogs.AddAsync(audit);

        await _context.SaveChangesAsync();

        return MapToDto(assessment, request);
    }

    public async Task<PagedResult<PriorityAssessmentDto>> GetPriorityAssessmentsAsync(PriorityAssessmentSearchFilterDto filter)
    {
        return await SearchPriorityAssessmentsAsync(filter);
    }

    public async Task<PagedResult<PriorityAssessmentDto>> SearchPriorityAssessmentsAsync(PriorityAssessmentSearchFilterDto filter)
    {
        var baseQuery = _context.Set<PriorityAssessment>()
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Priority))
        {
            baseQuery = baseQuery.Where(p => p.Priority == filter.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.RiskLevel))
        {
            baseQuery = baseQuery.Where(p => p.RiskLevel == filter.RiskLevel);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssetCriticality))
        {
            baseQuery = baseQuery.Where(p => p.AssetCriticality == filter.AssetCriticality);
        }

        if (filter.EscalatedOnly == true)
        {
            baseQuery = baseQuery.Where(p => p.EscalationFlag);
        }

        // Database join to allow SearchTerm filtering BEFORE CountAsync and Skip/Take
        var joinedQuery = from p in baseQuery
                          join r in _context.MaintenanceRequests.Where(req => !req.IsDeleted)
                              on p.RequestId equals r.Id into reqGroup
                          from req in reqGroup.DefaultIfEmpty()
                          select new { Assessment = p, Request = req };

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            joinedQuery = joinedQuery.Where(x =>
                (x.Request != null && x.Request.RequestNumber.ToLower().Contains(term)) ||
                (x.Request != null && x.Request.Title.ToLower().Contains(term)) ||
                (x.Assessment.Explanation != null && x.Assessment.Explanation.ToLower().Contains(term)));
        }

        // 1. Calculate TotalCount AFTER search & filters
        int totalCount = await joinedQuery.CountAsync();
        int page = filter.Page > 0 ? filter.Page : 1;
        int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        // 2. Apply deterministic sorting, then pagination AFTER filtering
        var sortBy = (filter.SortBy ?? string.Empty).Trim().ToLowerInvariant();
        var sorted = sortBy switch
        {
            "oldest" => joinedQuery.OrderBy(x => x.Assessment.CreatedAt),
            "risk_desc" or "highest_risk" => joinedQuery.OrderByDescending(x => x.Assessment.RiskScore),
            "risk_asc" or "lowest_risk" => joinedQuery.OrderBy(x => x.Assessment.RiskScore),
            "priority_desc" or "highest_priority" => joinedQuery
                .OrderByDescending(x => x.Assessment.Priority == "Critical" ? 4
                    : x.Assessment.Priority == "High" ? 3
                    : x.Assessment.Priority == "Medium" ? 2 : 1),
            "priority_asc" or "lowest_priority" => joinedQuery
                .OrderBy(x => x.Assessment.Priority == "Critical" ? 4
                    : x.Assessment.Priority == "High" ? 3
                    : x.Assessment.Priority == "Medium" ? 2 : 1),
            _ => joinedQuery.OrderByDescending(x => x.Assessment.CreatedAt) // newest first
        };

        var pageItems = await sorted
            .ThenBy(x => x.Assessment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestIds = pageItems.Select(x => x.Assessment.RequestId).Distinct().ToList();
        var fullRequests = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Where(r => requestIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        var dtos = pageItems.Select(x =>
        {
            fullRequests.TryGetValue(x.Assessment.RequestId, out var req);
            return MapToDto(x.Assessment, req ?? x.Request);
        }).ToList();

        return new PagedResult<PriorityAssessmentDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PriorityAssessmentDto> EscalateRequestAsync(Guid requestId, EscalateRequestDto dto, string escalatedBy)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request with ID '{requestId}' was not found.");
        }

        var assessment = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == requestId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (assessment == null)
        {
            // Run the real deterministic assessment first — never fabricate a risk score.
            await CreatePriorityAssessmentAsync(requestId, new CreatePriorityAssessmentDto
            {
                HasSafetyHazard = dto.ImmediateHazard ? true : null,
                Notes = dto.Notes
            }, escalatedBy);

            assessment = await _context.Set<PriorityAssessment>()
                .Where(p => p.RequestId == requestId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .FirstAsync();
        }

        var storedFactors = DeserializeFactors(assessment.ContributingFactorsJson);
        bool hazardRecalculated = false;

        if (dto.ImmediateHazard)
        {
            // A manager-confirmed immediate hazard is a new fact: deterministically
            // recalculate the risk assessment with the hazard included.
            string criticality = NormalizeLevel(assessment.AssetCriticality) ?? AssessAssetCriticality(request.Asset?.Criticality, request.Asset?.Category);
            string likelihood = NormalizeLevel(assessment.LikelihoodLevel) ?? "Medium";
            int recentFailures = storedFactors.RecentFailureCount;
            bool isHighDensity = storedFactors.LocationModifier >= 5;
            const string impact = "Critical"; // AssessImpact rule: active hazard forces Critical impact

            var (hazardScore, hazardFactors) = CalculateRiskScore(criticality, impact, likelihood, true, recentFailures, isHighDensity);
            var hazardLevel = DetermineRiskLevel(hazardScore);

            assessment.AssetCriticality = criticality;
            assessment.ImpactLevel = impact;
            assessment.LikelihoodLevel = likelihood;
            assessment.RiskScore = hazardScore;
            assessment.RiskLevel = hazardLevel;
            assessment.ContributingFactorsJson = JsonSerializer.Serialize(hazardFactors);
            hazardRecalculated = true;
        }

        // Operational escalation (management decision).
        // RiskScore/RiskLevel remain the deterministic calculated risk unless a confirmed
        // hazard forced a genuine recalculation above; Priority/SLA/EscalationFlag express
        // the operational escalation state.
        assessment.EscalationFlag = true;
        assessment.EscalationReason = Truncate(dto.Reason, 500);
        assessment.Priority = "Critical";
        assessment.RecommendedResponseWindow = "Immediate (Within 1 hour)";
        assessment.ResponseTimeHours = 1;
        assessment.ResolutionTimeHours = 4;

        var slaConfig = await _context.SLAConfigurations
            .FirstOrDefaultAsync(s => s.PriorityLevel == "Critical" && !s.IsDeleted);
        if (slaConfig != null)
        {
            assessment.ResponseTimeHours = slaConfig.ResponseTimeHours;
            assessment.ResolutionTimeHours = slaConfig.ResolutionTimeHours;
        }

        assessment.Explanation = Truncate(hazardRecalculated
            ? $"Deterministic risk score {assessment.RiskScore}/100 ({assessment.RiskLevel}) recalculated with a manager-confirmed immediate safety hazard. Manual operational escalation to Critical priority by {escalatedBy}: {dto.Reason}"
            : $"Calculated risk {assessment.RiskScore}/100 ({assessment.RiskLevel}) is unchanged. Manual operational escalation to Critical priority by {escalatedBy}: {dto.Reason}", 2000) ?? string.Empty;
        assessment.AssessedBy = escalatedBy;
        assessment.Status = "Escalated";
        assessment.UpdatedAt = DateTime.UtcNow;

        // Write Audit Log
        var audit = new AuditLog
        {
            Action = "RequestEscalated",
            EntityName = "MaintenanceRequest",
            EntityId = requestId.ToString(),
            ChangesJson = JsonSerializer.Serialize(new
            {
                Reason = dto.Reason,
                ImmediateHazard = dto.ImmediateHazard,
                EscalatedBy = escalatedBy,
                HazardRecalculated = hazardRecalculated,
                RiskScore = assessment.RiskScore,
                RiskLevel = assessment.RiskLevel,
                Priority = assessment.Priority
            }),
            IpAddress = "System/Service"
        };
        await _context.AuditLogs.AddAsync(audit);

        await _context.SaveChangesAsync();

        _logger.LogWarning("Request {RequestId} escalated by {EscalatedBy}. Reason: {Reason}. HazardRecalculated: {HazardRecalculated}",
            requestId, escalatedBy, dto.Reason, hazardRecalculated);

        return MapToDto(assessment, request);
    }

    public async Task<PriorityAssessmentDto> DeEscalateRequestAsync(Guid requestId, DeEscalateRequestDto dto, string deEscalatedBy)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request with ID '{requestId}' was not found.");
        }

        var assessment = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == requestId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (assessment == null)
        {
            throw new NotFoundException($"Priority assessment for request ID '{requestId}' was not found.");
        }

        // Safety verification: if the request still contains an active physical hazard, reject de-escalation
        bool hasActiveHazard = DetectHazard(request.Title, request.Description);
        if (hasActiveHazard)
        {
            throw new InvalidOperationException("Cannot de-escalate: the maintenance request description contains an active physical safety hazard that must first be resolved.");
        }

        // Recompute the authoritative assessment deterministically from the stored factor
        // levels (with the manager-confirmed hazard removed). We recompute from stored factors
        // rather than re-parsing request text so the recalculated risk is reproducible and
        // every persisted field stays internally consistent (no stale data).
        var storedFactors = DeserializeFactors(assessment.ContributingFactorsJson);

        string criticality = NormalizeLevel(assessment.AssetCriticality)
            ?? AssessAssetCriticality(request.Asset?.Criticality, request.Category?.Name);
        string impact = NormalizeLevel(assessment.ImpactLevel) ?? "Low";
        string likelihood = NormalizeLevel(assessment.LikelihoodLevel) ?? "Low";
        int recentFailures = storedFactors.RecentFailureCount;
        bool isHighDensity = storedFactors.LocationModifier >= 5;

        // De-escalation removes the operational escalation and any manager-confirmed hazard flag,
        // so the deterministic recalculation runs with hasSafetyHazard = false.
        // NOTE: argument order is (criticality, impact, likelihood, hasSafetyHazard, recentFailures, isHighDensityLocation).
        var (score, factors) = CalculateRiskScore(criticality, impact, likelihood, false, recentFailures, isHighDensity);
        var riskLevel = DetermineRiskLevel(score);
        var (priority, respHours, resHours, respWindow, computedEscalation) =
            CalculatePriorityAndSLA(score, riskLevel, false, criticality, impact);

        // Honour SLA configuration overrides when present.
        var slaConfig = await _context.SLAConfigurations
            .FirstOrDefaultAsync(s => s.PriorityLevel == priority && !s.IsDeleted);
        if (slaConfig != null)
        {
            respHours = slaConfig.ResponseTimeHours;
            resHours = slaConfig.ResolutionTimeHours;
        }

        // Sync ALL assessment fields so no stale escalation state survives.
        assessment.AssetCriticality = criticality;
        assessment.ImpactLevel = impact;
        assessment.LikelihoodLevel = likelihood;
        assessment.RiskScore = score;
        assessment.RiskLevel = riskLevel;
        assessment.Priority = priority;
        assessment.ResponseTimeHours = respHours;
        assessment.ResolutionTimeHours = resHours;
        assessment.RecommendedResponseWindow = respWindow;
        assessment.ContributingFactorsJson = JsonSerializer.Serialize(factors);

        // EscalationFlag is now the deterministic flag: it stays true only if the recalculated
        // risk genuinely warrants escalation (e.g. Critical priority), not because of the removed
        // manual operational escalation.
        assessment.EscalationFlag = computedEscalation;
        assessment.EscalationReason = computedEscalation
            ? "Recalculated Critical risk after de-escalation"
            : null;
        assessment.Explanation = Truncate(
            $"De-escalated by {deEscalatedBy}: {dto.Reason}. Deterministic recalculation (hazard flag removed) produced risk score {score}/100 ({riskLevel}) and priority {priority}.",
            2000) ?? string.Empty;
        assessment.AssessedBy = $"{deEscalatedBy} (De-escalated)";
        assessment.Status = computedEscalation ? "Escalated" : "Active";
        assessment.UpdatedAt = DateTime.UtcNow;

        var audit = new AuditLog
        {
            Action = "RequestDeEscalated",
            EntityName = "MaintenanceRequest",
            EntityId = requestId.ToString(),
            ChangesJson = JsonSerializer.Serialize(new
            {
                Reason = dto.Reason,
                DeEscalatedBy = deEscalatedBy,
                AssetCriticality = criticality,
                ImpactLevel = impact,
                LikelihoodLevel = likelihood,
                RecalculatedScore = score,
                RecalculatedRiskLevel = riskLevel,
                RecalculatedPriority = priority,
                EscalationFlag = computedEscalation
            }),
            IpAddress = "System/Service"
        };
        await _context.AuditLogs.AddAsync(audit);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Request {RequestId} de-escalated by {DeEscalatedBy}. Recalculated Priority: {Priority}, Score: {Score}",
            requestId, deEscalatedBy, priority, score);

        return MapToDto(assessment, request);
    }

    public async Task<PriorityAssessmentDto> GetRiskAssessmentAsync(Guid requestId)
    {
        return await GetPriorityByRequestIdAsync(requestId);
    }

    public async Task<PriorityAssessmentDto> SaveAgentAssessmentAsync(Guid requestId, PriorityAgentResultDto agentResult, string assessedBy)
    {
        if (agentResult == null)
        {
            throw new InvalidOperationException("Agent result cannot be null.");
        }

        // Authoritative validation of agent result before database persistence
        if (agentResult.RiskScore < 1 || agentResult.RiskScore > 100)
        {
            throw new InvalidOperationException($"Invalid agent RiskScore {agentResult.RiskScore}. Must be between 1 and 100.");
        }

        string expectedRiskLevel = DetermineRiskLevel(agentResult.RiskScore);
        if (!string.Equals(expectedRiskLevel, agentResult.RiskLevel, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Inconsistent agent result: RiskScore {agentResult.RiskScore} maps to '{expectedRiskLevel}', but agent reported '{agentResult.RiskLevel}'.");
        }

        if (string.IsNullOrWhiteSpace(agentResult.Priority))
        {
            throw new InvalidOperationException("Agent result Priority cannot be empty.");
        }

        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request with ID '{requestId}' was not found.");
        }

        int respHours = agentResult.Sla?.ResponseHours ?? 4;
        int resHours = agentResult.Sla?.ResolutionHours ?? 24;

        var slaConfig = await _context.SLAConfigurations
            .FirstOrDefaultAsync(s => s.PriorityLevel == agentResult.Priority && !s.IsDeleted);

        if (slaConfig != null)
        {
            respHours = slaConfig.ResponseTimeHours;
            resHours = slaConfig.ResolutionTimeHours;
        }

        // Rebuild the complete, authoritative contributing-factor set from the agent's
        // reported levels. The score was already cross-verified against CalculateRiskScore
        // in PriorityAgentService, so recomputing the factors here is consistent and complete
        // (includes recurrence/location modifiers and recent-failure count).
        bool agentHazard = agentResult.ContributingFactors?.HasSafetyHazard ?? (agentResult.HazardFlag ?? false);
        bool agentHighDensity = (agentResult.ContributingFactors?.LocationModifier ?? 0) >= 5;
        int agentRecentFailures = agentResult.ContributingFactors?.RecentFailureCount ?? 0;
        var (_, factors) = CalculateRiskScore(
            agentResult.AssetCriticality,
            agentResult.ImpactLevel,
            agentResult.LikelihoodLevel,
            agentHazard,
            agentRecentFailures,
            agentHighDensity);

        var existing = await _context.Set<PriorityAssessment>()
            .Where(p => p.RequestId == requestId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        bool isNew = existing == null;
        var assessment = existing ?? new PriorityAssessment
        {
            RequestId = requestId
        };

        assessment.AssetCriticality = agentResult.AssetCriticality;
        assessment.ImpactLevel = agentResult.ImpactLevel;
        assessment.LikelihoodLevel = agentResult.LikelihoodLevel;
        assessment.RiskScore = agentResult.RiskScore;
        assessment.RiskLevel = agentResult.RiskLevel;
        assessment.Priority = agentResult.Priority;
        assessment.RecommendedResponseWindow = agentResult.RecommendedResponseWindow;
        assessment.ResponseTimeHours = respHours;
        assessment.ResolutionTimeHours = resHours;
        assessment.EscalationFlag = agentResult.EscalationFlag;
        assessment.EscalationReason = agentResult.EscalationFlag ? "PriorityAgent critical risk threshold or safety hazard detected" : null;
        assessment.Explanation = agentResult.Explanation;
        assessment.ContributingFactorsJson = JsonSerializer.Serialize(factors);
        assessment.AssessedBy = assessedBy;
        assessment.Status = agentResult.EscalationFlag ? "Escalated" : "Active";

        if (isNew)
        {
            await _context.Set<PriorityAssessment>().AddAsync(assessment);
        }
        else
        {
            assessment.UpdatedAt = DateTime.UtcNow;
        }

        if (request.Status == RequestStatus.Submitted || request.Status == RequestStatus.Classified)
        {
            request.Status = RequestStatus.PriorityAssigned;
            request.UpdatedAt = DateTime.UtcNow;
        }

        var audit = new AuditLog
        {
            Action = isNew ? "PriorityAgentAssessmentCreated" : "PriorityAgentAssessmentUpdated",
            EntityName = "PriorityAssessment",
            EntityId = assessment.Id.ToString(),
            ChangesJson = JsonSerializer.Serialize(new
            {
                RequestId = requestId,
                Score = agentResult.RiskScore,
                Priority = agentResult.Priority,
                RiskLevel = agentResult.RiskLevel,
                AssessedBy = assessedBy
            }),
            IpAddress = "PriorityAgent/Service"
        };
        await _context.AuditLogs.AddAsync(audit);

        await _context.SaveChangesAsync();

        _logger.LogInformation("{Action} PriorityAgent assessment {Id} for Request {RequestId}: Score={Score}, Priority={Priority}",
            isNew ? "Persisted" : "Updated", assessment.Id, requestId, agentResult.RiskScore, agentResult.Priority);

        return MapToDto(assessment, request, factors);
    }

    #endregion

    #region Helpers

    private static PriorityAssessmentDto MapToDto(PriorityAssessment p, MaintenanceRequest? r, ContributingFactorsDto? factors = null)
    {
        ContributingFactorsDto factorsDto = factors ?? new ContributingFactorsDto();

        if (factors == null && !string.IsNullOrWhiteSpace(p.ContributingFactorsJson))
        {
            try
            {
                factorsDto = JsonSerializer.Deserialize<ContributingFactorsDto>(p.ContributingFactorsJson) ?? new ContributingFactorsDto();
            }
            catch
            {
                factorsDto = new ContributingFactorsDto();
            }
        }

        return new PriorityAssessmentDto
        {
            Id = p.Id,
            RequestId = p.RequestId,
            RequestNumber = r?.RequestNumber ?? string.Empty,
            RequestTitle = r?.Title ?? string.Empty,
            AssetName = r?.Asset?.Name,
            LocationName = r?.Location?.Name,
            AssetCriticality = p.AssetCriticality,
            ImpactLevel = p.ImpactLevel,
            LikelihoodLevel = p.LikelihoodLevel,
            RiskScore = p.RiskScore,
            RiskLevel = p.RiskLevel,
            Priority = p.Priority,
            RecommendedResponseWindow = p.RecommendedResponseWindow,
            ResponseTimeHours = p.ResponseTimeHours,
            ResolutionTimeHours = p.ResolutionTimeHours,
            EscalationFlag = p.EscalationFlag,
            EscalationReason = p.EscalationReason,
            Explanation = p.Explanation,
            ContributingFactors = factorsDto,
            AssessedBy = p.AssessedBy,
            Status = p.Status,
            HazardDetected = factorsDto.HasSafetyHazard,
            HumanApprovalRequired = p.Priority == "Critical" || factorsDto.HasSafetyHazard,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }

    private static string? NormalizeLevel(string level)
    {
        if (string.Equals(level, "Critical", StringComparison.OrdinalIgnoreCase)) return "Critical";
        if (string.Equals(level, "High", StringComparison.OrdinalIgnoreCase)) return "High";
        if (string.Equals(level, "Medium", StringComparison.OrdinalIgnoreCase)) return "Medium";
        if (string.Equals(level, "Low", StringComparison.OrdinalIgnoreCase)) return "Low";
        return null;
    }

    private static int LevelToPoints(string level)
    {
        return level switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            _ => 1
        };
    }

    /// <summary>
    /// Safely deserializes a stored ContributingFactorsJson blob. Never throws: falls back
    /// to an empty factor set so escalation/de-escalation can recompute from authoritative rules.
    /// </summary>
    private static ContributingFactorsDto DeserializeFactors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new ContributingFactorsDto();
        try
        {
            return JsonSerializer.Deserialize<ContributingFactorsDto>(json) ?? new ContributingFactorsDto();
        }
        catch
        {
            return new ContributingFactorsDto();
        }
    }

    /// <summary>
    /// Truncates free-text fields to a safe maximum length before persistence.
    /// </summary>
    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    #endregion
}

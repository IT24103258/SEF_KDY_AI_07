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
    /// </summary>
    public string AssessImpact(string? impactOverride, bool hasSafetyHazard, string? disruptionScope)
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

        if (string.Equals(disruptionScope, "Multi-Unit", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }

        return "Low";
    }

    /// <summary>
    /// Assesses the likelihood of incident escalation or recurrence based on historical frequency.
    /// </summary>
    public string AssessLikelihood(string? likelihoodOverride, int recentFailures, int openRequests)
    {
        if (!string.IsNullOrWhiteSpace(likelihoodOverride))
        {
            var normalized = NormalizeLevel(likelihoodOverride);
            if (normalized != null) return normalized;
        }

        int riskSignals = recentFailures + openRequests;

        if (riskSignals >= 4) return "Critical";
        if (riskSignals >= 2) return "High";
        if (riskSignals == 1) return "Medium";

        return "Low";
    }

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
            // If not yet assessed, perform an on-demand calculation
            return await CreatePriorityAssessmentAsync(requestId, null, "Auto-Evaluator");
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

        // Assess input factors
        string criticality = AssessAssetCriticality(
            dto?.AssetCriticalityOverride ?? request.Asset?.Criticality,
            request.Asset?.Category);

        bool hasSafetyHazard = dto?.HasSafetyHazard ?? (
            request.Title.Contains("gas", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("spark", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("smoke", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("leak", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("hazard", StringComparison.OrdinalIgnoreCase));

        string impact = AssessImpact(dto?.ImpactOverride, hasSafetyHazard, dto?.DisruptionScope);

        // Count open requests for this asset
        int openRequestsForAsset = 0;
        if (request.AssetId.HasValue)
        {
            openRequestsForAsset = await _context.MaintenanceRequests
                .CountAsync(r => r.AssetId == request.AssetId && r.Id != request.Id && r.Status != RequestStatus.Completed && r.Status != RequestStatus.Cancelled);
        }

        string likelihood = AssessLikelihood(dto?.LikelihoodOverride, 0, openRequestsForAsset);

        bool isHighDensity = request.Location?.Building == "Common Areas" ||
                             (request.Location?.Room != null && request.Location.Room.Contains("Lobby", StringComparison.OrdinalIgnoreCase));

        var (score, factors) = CalculateRiskScore(criticality, impact, likelihood, hasSafetyHazard, 0, isHighDensity);
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

        if (!string.IsNullOrWhiteSpace(dto.Priority))
        {
            assessment.Priority = dto.Priority;
        }

        if (!string.IsNullOrWhiteSpace(dto.RiskLevel))
        {
            assessment.RiskLevel = dto.RiskLevel;
        }

        if (dto.EscalationFlag.HasValue)
        {
            assessment.EscalationFlag = dto.EscalationFlag.Value;
        }

        if (dto.EscalationReason != null)
        {
            assessment.EscalationReason = dto.EscalationReason;
        }

        if (!string.IsNullOrWhiteSpace(dto.Explanation))
        {
            assessment.Explanation = dto.Explanation;
        }

        assessment.Status = "Overridden";
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
        var query = _context.Set<PriorityAssessment>()
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Priority))
        {
            query = query.Where(p => p.Priority == filter.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.RiskLevel))
        {
            query = query.Where(p => p.RiskLevel == filter.RiskLevel);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssetCriticality))
        {
            query = query.Where(p => p.AssetCriticality == filter.AssetCriticality);
        }

        if (filter.EscalatedOnly == true)
        {
            query = query.Where(p => p.EscalationFlag);
        }

        int totalCount = await query.CountAsync();
        int page = filter.Page > 0 ? filter.Page : 1;
        int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestIds = items.Select(i => i.RequestId).Distinct().ToList();
        var requests = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Where(r => requestIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        var dtos = items.Select(item =>
        {
            requests.TryGetValue(item.RequestId, out var req);
            return MapToDto(item, req);
        }).ToList();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            dtos = dtos.Where(d =>
                d.RequestNumber.ToLower().Contains(term) ||
                d.RequestTitle.ToLower().Contains(term) ||
                (d.AssetName != null && d.AssetName.ToLower().Contains(term)) ||
                d.Explanation.ToLower().Contains(term)).ToList();
        }

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
            // Create assessment first with hazard flag
            var createDto = new CreatePriorityAssessmentDto
            {
                HasSafetyHazard = dto.ImmediateHazard,
                Notes = dto.Notes
            };
            assessment = new PriorityAssessment
            {
                RequestId = requestId,
                RiskScore = dto.ImmediateHazard ? 85 : 75,
                RiskLevel = "Critical",
                Priority = "Critical",
                EscalationFlag = true,
                EscalationReason = dto.Reason,
                RecommendedResponseWindow = "Immediate (Within 1 hour)",
                ResponseTimeHours = 1,
                ResolutionTimeHours = 4,
                Explanation = $"Manual escalation triggered by {escalatedBy}: {dto.Reason}",
                AssessedBy = escalatedBy,
                Status = "Escalated"
            };
            await _context.Set<PriorityAssessment>().AddAsync(assessment);
        }
        else
        {
            assessment.EscalationFlag = true;
            assessment.EscalationReason = dto.Reason;
            assessment.Status = "Escalated";
            assessment.Priority = "Critical";
            assessment.RecommendedResponseWindow = "Immediate (Within 1 hour)";
            assessment.ResponseTimeHours = 1;
            assessment.ResolutionTimeHours = 4;
            assessment.Explanation += $" | Escalated by {escalatedBy}: {dto.Reason}";
            assessment.UpdatedAt = DateTime.UtcNow;
        }

        // Write Audit Log
        var audit = new AuditLog
        {
            Action = "RequestEscalated",
            EntityName = "MaintenanceRequest",
            EntityId = requestId.ToString(),
            ChangesJson = JsonSerializer.Serialize(new { Reason = dto.Reason, ImmediateHazard = dto.ImmediateHazard, EscalatedBy = escalatedBy }),
            IpAddress = "System/Service"
        };
        await _context.AuditLogs.AddAsync(audit);

        await _context.SaveChangesAsync();

        _logger.LogWarning("Request {RequestId} escalated by {EscalatedBy}. Reason: {Reason}", requestId, escalatedBy, dto.Reason);

        return MapToDto(assessment, request);
    }

    public async Task<PriorityAssessmentDto> GetRiskAssessmentAsync(Guid requestId)
    {
        return await GetPriorityByRequestIdAsync(requestId);
    }

    public async Task<PriorityAssessmentDto> SaveAgentAssessmentAsync(Guid requestId, PriorityAgentResultDto agentResult, string assessedBy)
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

        int respHours = agentResult.Sla?.ResponseHours ?? 4;
        int resHours = agentResult.Sla?.ResolutionHours ?? 24;

        var slaConfig = await _context.SLAConfigurations
            .FirstOrDefaultAsync(s => s.PriorityLevel == agentResult.Priority && !s.IsDeleted);

        if (slaConfig != null)
        {
            respHours = slaConfig.ResponseTimeHours;
            resHours = slaConfig.ResolutionTimeHours;
        }

        var factors = new ContributingFactorsDto
        {
            AssetCriticality = agentResult.AssetCriticality,
            ImpactScore = LevelToPoints(agentResult.ImpactLevel),
            LikelihoodScore = LevelToPoints(agentResult.LikelihoodLevel),
            AssetCriticalityScore = LevelToPoints(agentResult.AssetCriticality) * 7,
            BaseMatrixScore = LevelToPoints(agentResult.ImpactLevel) * LevelToPoints(agentResult.LikelihoodLevel) * 4,
            HasSafetyHazard = agentResult.HazardFlag ?? false,
            SafetyHazardModifier = (agentResult.HazardFlag == true) ? 25 : 0,
            OperationalDisruption = agentResult.ImpactLevel
        };

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

    #endregion
}

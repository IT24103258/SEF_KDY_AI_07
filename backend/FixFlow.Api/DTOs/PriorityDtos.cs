namespace FixFlow.Api.DTOs;

public class PriorityAssessmentDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string? LocationName { get; set; }
    public string AssetCriticality { get; set; } = "Medium";
    public string ImpactLevel { get; set; } = "Medium";
    public string LikelihoodLevel { get; set; } = "Medium";
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Medium";
    public string Priority { get; set; } = "Medium";
    public string RecommendedResponseWindow { get; set; } = "Within 4 hours";
    public int ResponseTimeHours { get; set; } = 4;
    public int ResolutionTimeHours { get; set; } = 24;
    public bool EscalationFlag { get; set; }
    public string? EscalationReason { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public ContributingFactorsDto ContributingFactors { get; set; } = new();
    public string AssessedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool HazardDetected { get; set; }
    public bool HumanApprovalRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ContributingFactorsDto
{
    public string AssetCriticality { get; set; } = "Medium";
    public int BaseMatrixScore { get; set; }
    public int AssetCriticalityScore { get; set; }
    public int ImpactScore { get; set; }
    public int LikelihoodScore { get; set; }
    public bool HasSafetyHazard { get; set; }
    public int SafetyHazardModifier { get; set; }
    public int RecurrenceModifier { get; set; }
    public int LocationModifier { get; set; }
    public int RecentFailureCount { get; set; }
    public string OperationalDisruption { get; set; } = "Normal";
}

/// <summary>
/// Component 1 -> Component 2 Input Contract.
/// Component 1 supplies raw incident/request facts; Component 2 determines risk and priority.
/// </summary>
public class CreatePriorityAssessmentDto
{
    // Raw incident / request facts supplied by Component 1
    public string? AssetCategory { get; set; }
    public string? DisruptionInformation { get; set; }
    public string? DisruptionScope { get; set; }
    public string? FailureHistory { get; set; }
    public int? RecentFailureCount { get; set; }
    public string? LocationInfo { get; set; }
    public bool? HighDensityLocation { get; set; }
    public string? HazardDetails { get; set; }
    public string? Notes { get; set; }

    // Controlled / Test-only overrides (optional internal/test controls for golden test calibration).
    // These are NOT required in the normal Component 1 production contract.
    public string? AssetCriticalityOverride { get; set; }
    public string? ImpactOverride { get; set; }
    public string? LikelihoodOverride { get; set; }
    public bool? HasSafetyHazard { get; set; }
}

public class UpdatePriorityAssessmentDto
{
    public string? Priority { get; set; }
    public string? RiskLevel { get; set; }
    public bool? EscalationFlag { get; set; }
    public string? EscalationReason { get; set; }
    public string? Explanation { get; set; }
}

public class EscalateRequestDto
{
    public string Reason { get; set; } = string.Empty;
    public bool ImmediateHazard { get; set; } = false;
    public string? Notes { get; set; }
}

public class DeEscalateRequestDto
{
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class RiskSimulationRequestDto
{
    public string? AssetCriticality { get; set; }
    public string? ImpactLevel { get; set; }
    public string? LikelihoodLevel { get; set; }
    public bool? HasSafetyHazard { get; set; }
    public int? RecentFailureCount { get; set; }
    public bool? IsHighDensityLocation { get; set; }
}

public class RiskSimulationResultDto
{
    public Guid RequestId { get; set; }
    public PriorityAssessmentDto? BaselineAssessment { get; set; }
    public PriorityAssessmentDto SimulatedAssessment { get; set; } = new();
    public RiskSimulationDeltaDto Delta { get; set; } = new();
    public bool IsSimulated => true;
}

public class RiskSimulationDeltaDto
{
    public int ScoreDelta { get; set; }
    public bool RiskLevelChanged { get; set; }
    public string OriginalRiskLevel { get; set; } = string.Empty;
    public string SimulatedRiskLevel { get; set; } = string.Empty;
    public bool PriorityChanged { get; set; }
    public string OriginalPriority { get; set; } = string.Empty;
    public string SimulatedPriority { get; set; } = string.Empty;
    public bool EscalationStateChanged { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class PriorityAssessmentSearchFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Priority { get; set; }
    public string? RiskLevel { get; set; }
    public bool? EscalatedOnly { get; set; }
    public string? AssetCriticality { get; set; }
    /// <summary>
    /// Deterministic sort key: newest (default), oldest, risk_desc/risk_asc, priority_desc/priority_asc.
    /// </summary>
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}



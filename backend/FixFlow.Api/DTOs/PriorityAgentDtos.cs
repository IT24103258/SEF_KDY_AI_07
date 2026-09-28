using System.Text.Json;
using System.Text.Json.Serialization;

namespace FixFlow.Api.DTOs;

public class PriorityAgentResultDto
{
    [JsonPropertyName("asset_criticality")]
    public string AssetCriticality { get; set; } = "Medium";

    [JsonPropertyName("impact_level")]
    public string ImpactLevel { get; set; } = "Medium";

    [JsonPropertyName("likelihood_level")]
    public string LikelihoodLevel { get; set; } = "Medium";

    [JsonPropertyName("risk_score")]
    public int RiskScore { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = "Medium";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "Medium";

    [JsonPropertyName("recommended_response_window")]
    public string RecommendedResponseWindow { get; set; } = "Within 4 hours";

    [JsonPropertyName("sla")]
    public PriorityAgentSlaDto Sla { get; set; } = new();

    [JsonPropertyName("escalation_flag")]
    public bool EscalationFlag { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("priority_level")]
    public string? PriorityLevel { get; set; }

    [JsonPropertyName("target_sla_hours")]
    public int? TargetSlaHours { get; set; }

    [JsonPropertyName("hazard_flag")]
    public bool? HazardFlag { get; set; }

    [JsonPropertyName("hazard_detected")]
    public bool? HazardDetected { get; set; }

    [JsonPropertyName("human_approval_required")]
    public bool? HumanApprovalRequired { get; set; }

    /// <summary>
    /// Deterministic contributing factors returned by the Python PriorityAgent.
    /// Mirrors C# ContributingFactorsDto so the score can be independently re-verified.
    /// </summary>
    [JsonPropertyName("contributing_factors")]
    public PriorityAgentContributingFactorsDto? ContributingFactors { get; set; }

    /// <summary>Safe-failure signalling fields (present when status == FAILED).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("approval_reason")]
    public string? ApprovalReason { get; set; }
}

/// <summary>
/// Snake-case contract for the Python PriorityAgent's contributing_factors block.
/// </summary>
public class PriorityAgentContributingFactorsDto
{
    [JsonPropertyName("asset_criticality")]
    public string AssetCriticality { get; set; } = "Low";

    [JsonPropertyName("base_matrix_score")]
    public int BaseMatrixScore { get; set; }

    [JsonPropertyName("asset_criticality_score")]
    public int AssetCriticalityScore { get; set; }

    [JsonPropertyName("impact_score")]
    public int ImpactScore { get; set; }

    [JsonPropertyName("likelihood_score")]
    public int LikelihoodScore { get; set; }

    [JsonPropertyName("has_safety_hazard")]
    public bool HasSafetyHazard { get; set; }

    [JsonPropertyName("safety_hazard_modifier")]
    public int SafetyHazardModifier { get; set; }

    [JsonPropertyName("recurrence_modifier")]
    public int RecurrenceModifier { get; set; }

    [JsonPropertyName("location_modifier")]
    public int LocationModifier { get; set; }

    [JsonPropertyName("recent_failure_count")]
    public int RecentFailureCount { get; set; }

    [JsonPropertyName("operational_disruption")]
    public string OperationalDisruption { get; set; } = string.Empty;
}

public class PriorityAgentSlaDto
{
    [JsonPropertyName("response_hours")]
    public int ResponseHours { get; set; } = 4;

    [JsonPropertyName("resolution_hours")]
    public int ResolutionHours { get; set; } = 24;
}

/// <summary>
/// Component 1 -> Component 2 Agent Request Contract.
/// Component 1 supplies raw factual incident data; PriorityAgent assesses risk factors.
/// </summary>
public class PriorityAgentEvaluationRequestDto
{
    // Raw factual fields from Component 1
    public string? AssetCategory { get; set; }
    public string? DisruptionInformation { get; set; }
    public string? DisruptionScope { get; set; }
    public string? FailureHistory { get; set; }
    public int? RecentFailureCount { get; set; }
    public string? LocationInfo { get; set; }
    public bool? HighDensityLocation { get; set; }
    public string? HazardDetails { get; set; }
    public string? Notes { get; set; }

    // Controlled / Test-only overrides (optional internal controls for golden test calibration)
    public string? AssetCriticalityOverride { get; set; }
    public string? ImpactOverride { get; set; }
    public string? LikelihoodOverride { get; set; }
    public bool? HasSafetyHazard { get; set; }
}

public class PriorityAgentWorkflowExecutionRequest
{
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("workflow_type")]
    public string WorkflowType { get; set; } = "Priority";

    [JsonPropertyName("input_context")]
    public Dictionary<string, object?> InputContext { get; set; } = new();
}

public class PriorityAgentWorkflowExecutionResult
{
    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; } = string.Empty;

    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("plan")]
    public List<string> Plan { get; set; } = new();

    [JsonPropertyName("workflow_type")]
    public string? WorkflowType { get; set; }

    [JsonPropertyName("steps")]
    public List<PriorityAgentWorkflowStepResult> Steps { get; set; } = new();

    [JsonPropertyName("requires_human_approval")]
    public bool RequiresHumanApproval { get; set; }

    [JsonPropertyName("approval_reason")]
    public string? ApprovalReason { get; set; }
}

public class PriorityAgentWorkflowStepResult
{
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("step_name")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("output_data")]
    public PriorityAgentResultDto? OutputData { get; set; }

    [JsonPropertyName("validation_passed")]
    public bool ValidationPassed { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<PriorityAgentToolCallDto> ToolCalls { get; set; } = new();
}

/// <summary>
/// Snake-case contract for a single allow-listed tool invocation logged by an agent step.
/// Persisted to AgentToolCall for full workflow auditability.
/// </summary>
public class PriorityAgentToolCallDto
{
    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("input_params")]
    public JsonElement? InputParams { get; set; }

    [JsonPropertyName("output_params")]
    public JsonElement? OutputParams { get; set; }

    [JsonPropertyName("execution_time_ms")]
    public int ExecutionTimeMs { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

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
}

public class PriorityAgentSlaDto
{
    [JsonPropertyName("response_hours")]
    public int ResponseHours { get; set; } = 4;

    [JsonPropertyName("resolution_hours")]
    public int ResolutionHours { get; set; } = 24;
}

public class PriorityAgentEvaluationRequestDto
{
    public string? AssetCriticalityOverride { get; set; }
    public string? ImpactOverride { get; set; }
    public string? LikelihoodOverride { get; set; }
    public bool? HasSafetyHazard { get; set; }
    public string? DisruptionScope { get; set; }
    public string? Notes { get; set; }
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
}

namespace FixFlow.Api.Models;

public class PriorityAssessment : BaseEntity
{
    public Guid RequestId { get; set; }
    public string AssetCriticality { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string ImpactLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string LikelihoodLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
    public int RiskScore { get; set; } // 1 to 100
    public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string RecommendedResponseWindow { get; set; } = "Within 4 hours";
    public int ResponseTimeHours { get; set; } = 4;
    public int ResolutionTimeHours { get; set; } = 24;
    public bool EscalationFlag { get; set; } = false;
    public string? EscalationReason { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string ContributingFactorsJson { get; set; } = "{}";
    public string AssessedBy { get; set; } = "PriorityAgent";
    public string Status { get; set; } = "Active"; // Active, Escalated, Overridden
}

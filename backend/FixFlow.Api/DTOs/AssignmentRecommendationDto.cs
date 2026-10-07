namespace FixFlow.Api.DTOs;

public class AssignmentRecommendationDto
{
    public Guid RequestId { get; set; }
    public Guid RecommendedTechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public string Status { get; set; } = "Recommended";
}
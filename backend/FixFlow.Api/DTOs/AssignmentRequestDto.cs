namespace FixFlow.Api.DTOs;

public class AssignmentRequestDto
{
    public int RequestId { get; set; } = 101;
    public string RequiredSkill { get; set; } = "Plumbing";
    public string PriorityLevel { get; set; } = "High";

    // Add this property to resolve Build Error CS1061.
    public string? TechnicianId { get; set; } = "TECH-001";
}
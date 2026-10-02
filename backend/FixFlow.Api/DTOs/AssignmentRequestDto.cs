namespace FixFlow.Api.DTOs;

public class AssignmentRequestDto
{
    public string RequestId { get; set; } = string.Empty; 
    public string RequiredSkill { get; set; } = string.Empty; 
    public string PriorityLevel { get; set; } = string.Empty; 
    public string TechnicianId { get; set; } = string.Empty;
}
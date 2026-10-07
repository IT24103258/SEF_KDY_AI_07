namespace FixFlow.Api.DTOs;

public class AssignmentRequestDto
{
    public Guid RequestId { get; set; }
    public string RequiredSkill { get; set; } = string.Empty; 
    public string PriorityLevel { get; set; } = string.Empty; 
    public Guid TechnicianId { get; set; }
}
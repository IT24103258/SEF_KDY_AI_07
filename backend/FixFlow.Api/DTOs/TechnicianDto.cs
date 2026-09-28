namespace FixFlow.Api.DTOs;

public class TechnicianDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxDailyJobs { get; set; }
    public List<string> Skills { get; set; } = new();
}

public class CreateTechnicianDto
{
    public string UserId { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int MaxDailyJobs { get; set; } = 5;
    public List<int> SkillIds { get; set; } = new();
}
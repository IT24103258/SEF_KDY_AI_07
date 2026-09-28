namespace FixFlow.Api.Models;

public class Assignment : BaseEntity
{
    public int RequestId { get; set; }
    public MaintenanceRequest? MaintenanceRequest { get; set; }

    public Guid TechnicianId { get; set; } 
    public Technician Technician { get; set; } = null!;

    public double MatchScore { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public string Status { get; set; } = "Recommended";
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
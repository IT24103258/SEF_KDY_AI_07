namespace FixFlow.Api.Models;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Warning, Alert
    public bool IsRead { get; set; } = false;
}

public class SLAConfiguration : BaseEntity
{
    public string PriorityLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
    public int ResponseTimeHours { get; set; }
    public int ResolutionTimeHours { get; set; }
    public string EscalationEmail { get; set; } = string.Empty;
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

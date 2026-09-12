using FixFlow.Api.Models.Enums;

namespace FixFlow.Api.Models;

public class MaintenanceRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Submitted;
    
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }
    
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    
    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }
    
    public Guid? CategoryId { get; set; }
    public IssueCategory? Category { get; set; }

    public ICollection<AgentWorkflow> AgentWorkflows { get; set; } = new List<AgentWorkflow>();
}

public class IssueCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultPriority { get; set; } = "Medium";
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}

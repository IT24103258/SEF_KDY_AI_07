namespace FixFlow.Api.Models;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}

public class Asset : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Criticality { get; set; } = "Medium"; // Low, Medium, High, Critical
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}

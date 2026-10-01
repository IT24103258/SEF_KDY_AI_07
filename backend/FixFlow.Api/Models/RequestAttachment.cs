namespace FixFlow.Api.Models;

public class RequestAttachment : BaseEntity
{
    public Guid MaintenanceRequestId { get; set; }
    public MaintenanceRequest MaintenanceRequest { get; set; } = null!;

    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "image";
}
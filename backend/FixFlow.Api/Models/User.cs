namespace FixFlow.Api.Models;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Administrator, Manager, Technician, Requester
    public string Description { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}

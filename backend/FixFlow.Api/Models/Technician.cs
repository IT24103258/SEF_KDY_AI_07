namespace FixFlow.Api.Models;

public class Technician : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
}

public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ICollection<Technician> Technicians { get; set; } = new List<Technician>();
}

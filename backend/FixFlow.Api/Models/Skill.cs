namespace FixFlow.Api.Models;

public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public ICollection<Technician> Technicians { get; set; } = new List<Technician>();
}
namespace FixFlow.Api.Models;

public class TechnicianSkill
{
    public int TechnicianId { get; set; }
    public Technician Technician { get; set; } = null!;

    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    public int ProficiencyLevel { get; set; } = 1;
    public DateTime? CertifiedUntil { get; set; }
}
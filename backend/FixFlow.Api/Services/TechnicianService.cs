using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

public class TechnicianService : ITechnicianService
{
    private readonly FixFlowDbContext _context;
    private readonly HttpClient _httpClient;

    public TechnicianService(FixFlowDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    private static TechnicianDto MapToDto(Technician t)
    {
        return new TechnicianDto
        {
            Id = t.Id,
            UserId = t.UserId.ToString(),
            EmployeeCode = t.EmployeeId,
            FullName = t.User != null ? $"{t.User.FirstName} {t.User.LastName}" : t.EmployeeId,
            Phone = t.User?.PhoneNumber ?? "N/A",
            Status = t.IsAvailable ? "Available" : "Busy",
            MaxDailyJobs = 5,
            Skills = !string.IsNullOrEmpty(t.Specialization)
                ? new List<string> { t.Specialization }
                : t.Skills.Select(s => s.Name).ToList()
        };
    }

    /// <summary>
    /// Resolves a skill reference (Skill.Id, Skill.Name, or Skill.Category — the UI/AI
    /// uses category values like "Elevator/Lift") to the canonical Skill entity.
    /// </summary>
    private async Task<Skill?> ResolveSkillAsync(string? skillRef)
    {
        if (string.IsNullOrWhiteSpace(skillRef)) return null;

        var value = skillRef.Trim();

        if (Guid.TryParse(value, out var id))
        {
            var byId = await _context.Skills.FirstOrDefaultAsync(s => s.Id == id);
            if (byId != null) return byId;
        }

        return await _context.Skills
            .FirstOrDefaultAsync(s =>
                s.Name.ToLower() == value.ToLower() ||
                s.Category.ToLower() == value.ToLower());
    }

    private static (double Score, bool SkillMatched) EvaluateTechnician(Technician tech, string targetSkill, Skill? canonicalSkill)
    {
        double score = 0.0;
        bool matched = false;

        if (!string.IsNullOrEmpty(targetSkill))
        {
            if (!string.IsNullOrEmpty(tech.Specialization) &&
                (tech.Specialization.Contains(targetSkill, StringComparison.OrdinalIgnoreCase) ||
                 targetSkill.Contains(tech.Specialization, StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.80;
                matched = true;
            }

            if (tech.Skills != null && tech.Skills.Any(s =>
                (canonicalSkill != null && s.Id == canonicalSkill.Id) ||
                (!string.IsNullOrEmpty(s.Name) &&
                 (s.Name.Contains(targetSkill, StringComparison.OrdinalIgnoreCase) ||
                  targetSkill.Contains(s.Name, StringComparison.OrdinalIgnoreCase)))))
            {
                score += 0.90;
                matched = true;
            }
        }

        if (tech.IsAvailable)
        {
            score += 0.10;
        }

        return (score, matched);
    }

    private async Task<string?> GetRequiredSkillFromClassificationAsync(Guid maintenanceRequestId)
    {
        var classification = await _context.Set<RequestClassification>()
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(c => c.MaintenanceRequestId == maintenanceRequestId);

        return classification?.RequiredSkill;
    }

    public async Task<IEnumerable<TechnicianDto>> GetAllTechniciansAsync(string? skill)
    {
        var query = _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(skill))
        {
            query = query.Where(t => t.Skills.Any(s => s.Name.ToLower() == skill.ToLower()));
        }

        var technicians = await query.ToListAsync();

        return technicians.Select(MapToDto);
    }

    public async Task<TechnicianDto?> GetTechnicianByIdAsync(Guid id)
    {
        var tech = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tech == null) return null;

        return MapToDto(tech);
    }

    public async Task<TechnicianDto> CreateTechnicianAsync(CreateTechnicianDto dto)
    {
        Guid parsedUserId = Guid.TryParse(dto.UserId, out var result) ? result : Guid.NewGuid();

        var tech = new Technician
        {
            UserId = parsedUserId,
            EmployeeId = dto.EmployeeCode,
            Specialization = "General Repair",
            IsAvailable = true
        };

        if (dto.SkillIds != null && dto.SkillIds.Any())
        {
            var selectedSkills = new List<Skill>();
            foreach (var skillRef in dto.SkillIds.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var skill = await ResolveSkillAsync(skillRef);
                if (skill != null && selectedSkills.All(s => s.Id != skill.Id))
                {
                    selectedSkills.Add(skill);
                }
            }
            tech.Skills = selectedSkills;
        }

        _context.Technicians.Add(tech);
        await _context.SaveChangesAsync();

        var created = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .FirstAsync(t => t.Id == tech.Id);

        return MapToDto(created);
    }

    public async Task<AssignmentRecommendationDto> RecommendTechnicianAsync(AssignmentRequestDto requestDto)
    {
        var request = await _context.Set<MaintenanceRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestDto.RequestId);
        if (request == null)
        {
            throw new KeyNotFoundException($"Maintenance request '{requestDto.RequestId}' was not found.");
        }

        var technicians = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .ToListAsync();

        string requiredSkill = !string.IsNullOrWhiteSpace(requestDto.RequiredSkill)
            ? requestDto.RequiredSkill.Trim()
            : (await GetRequiredSkillFromClassificationAsync(request.Id)) ?? string.Empty;

        Skill? canonicalSkill = await ResolveSkillAsync(requiredSkill);

        IEnumerable<Technician> pool = technicians;
        if (!string.IsNullOrEmpty(requiredSkill))
        {
            pool = technicians
                .Where(t => EvaluateTechnician(t, requiredSkill, canonicalSkill).SkillMatched)
                .ToList();

            if (!pool.Any())
            {
                throw new InvalidOperationException($"No technician has the required skill '{requiredSkill}'.");
            }
        }

        Technician? bestMatch = null;
        double highestScore = 0.0;

        foreach (var tech in pool)
        {
            double currentScore = EvaluateTechnician(tech, requiredSkill, canonicalSkill).Score;

            if (currentScore > highestScore)
            {
                highestScore = currentScore;
                bestMatch = tech;
            }
        }

        if (bestMatch == null)
        {
            bestMatch = pool.First();
            highestScore = 0.50;
        }

        double finalScore = Math.Round(Math.Min(highestScore, 1.0), 2);

        string techFullName = bestMatch.User != null
            ? $"{bestMatch.User.FirstName} {bestMatch.User.LastName}"
            : bestMatch.EmployeeId;

        string matchType = finalScore >= 0.70 ? "AI Skill Match" : "General Availability Match";
        string reasoningSummary = $"{matchType}: Selected '{techFullName}' based on dynamic evaluation for skill '{requiredSkill}' with priority '{requestDto.PriorityLevel}'.";

        var existingRecommendation = await _context.Set<Assignment>()
            .Where(a => a.MaintenanceRequestId == request.Id && a.Status == "Recommended")
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (existingRecommendation != null)
        {
            existingRecommendation.TechnicianId = bestMatch.Id;
            existingRecommendation.MatchScore = finalScore;
            existingRecommendation.ReasoningSummary = reasoningSummary;
            existingRecommendation.AssignedAt = DateTime.UtcNow;
        }
        else
        {
            _context.Set<Assignment>().Add(new Assignment
            {
                Id = Guid.NewGuid(),
                MaintenanceRequestId = request.Id,
                TechnicianId = bestMatch.Id,
                MatchScore = finalScore,
                ReasoningSummary = reasoningSummary,
                Status = "Recommended",
                AssignedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return new AssignmentRecommendationDto
        {
            RequestId = request.Id,
            RecommendedTechnicianId = bestMatch.Id,
            TechnicianName = techFullName,
            MatchScore = finalScore,
            ReasoningSummary = reasoningSummary,
            Status = "Recommended"
        };
    }

    public async Task<bool> AssignTechnicianAsync(Guid requestId, Guid technicianId)
    {
        var request = await _context.Set<MaintenanceRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
        {
            throw new KeyNotFoundException($"Maintenance request '{requestId}' was not found.");
        }

        var tech = await _context.Technicians
            .Include(t => t.Skills)
            .FirstOrDefaultAsync(t => t.Id == technicianId);
        if (tech == null)
        {
            throw new KeyNotFoundException($"Technician '{technicianId}' was not found.");
        }

        string requiredSkill = (await GetRequiredSkillFromClassificationAsync(request.Id)) ?? string.Empty;
        Skill? canonicalSkill = await ResolveSkillAsync(requiredSkill);

        if (!string.IsNullOrEmpty(requiredSkill) && !EvaluateTechnician(tech, requiredSkill, canonicalSkill).SkillMatched)
        {
            throw new InvalidOperationException($"Technician '{tech.EmployeeId}' does not have the required skill '{requiredSkill}'.");
        }

        var assignment = await _context.Set<Assignment>()
            .Where(a => a.MaintenanceRequestId == request.Id)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (assignment != null)
        {
            assignment.TechnicianId = tech.Id;
            assignment.Status = "Assigned";
            assignment.AssignedAt = DateTime.UtcNow;
        }
        else
        {
            var evaluation = EvaluateTechnician(tech, requiredSkill, canonicalSkill);
            double finalScore = Math.Round(Math.Min(evaluation.Score, 1.0), 2);

            _context.Set<Assignment>().Add(new Assignment
            {
                Id = Guid.NewGuid(),
                MaintenanceRequestId = request.Id,
                TechnicianId = tech.Id,
                MatchScore = finalScore,
                ReasoningSummary = evaluation.SkillMatched
                    ? $"Manager-approved assignment for skill '{requiredSkill}'."
                    : "Manager-approved assignment (no specific skill requirement).",
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow
            });
        }

        return await _context.SaveChangesAsync() > 0;
    }
}

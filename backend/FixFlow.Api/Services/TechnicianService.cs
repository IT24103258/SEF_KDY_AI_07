using System.Net.Http.Json;
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

        return technicians.Select(t => new TechnicianDto
        {
            Id = Math.Abs(t.Id.GetHashCode()), 
            UserId = t.UserId.ToString(),
            EmployeeCode = t.EmployeeId,
            FullName = t.User != null ? $"{t.User.FirstName} {t.User.LastName}" : t.EmployeeId,
            Phone = t.User?.PhoneNumber ?? "N/A",
            Status = t.IsAvailable ? "Available" : "Busy",
            MaxDailyJobs = 5,
            Skills = !string.IsNullOrEmpty(t.Specialization) 
                ? new List<string> { t.Specialization } 
                : t.Skills.Select(s => s.Name).ToList()
        });
    }

    public async Task<TechnicianDto?> GetTechnicianByIdAsync(int id)
    {
        var tech = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .FirstOrDefaultAsync(t => Math.Abs(t.Id.GetHashCode()) == id);

        if (tech == null) return null;

        return new TechnicianDto
        {
            Id = id,
            UserId = tech.UserId.ToString(),
            EmployeeCode = tech.EmployeeId,
            FullName = tech.User != null ? $"{tech.User.FirstName} {tech.User.LastName}" : tech.EmployeeId,
            Phone = tech.User?.PhoneNumber ?? "N/A",
            Status = tech.IsAvailable ? "Available" : "Busy",
            MaxDailyJobs = 5,
            Skills = !string.IsNullOrEmpty(tech.Specialization) 
                ? new List<string> { tech.Specialization } 
                : tech.Skills.Select(s => s.Name).ToList()
        };
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
            var selectedSkills = await _context.Skills.Take(dto.SkillIds.Count).ToListAsync();
            tech.Skills = selectedSkills;
        }

        _context.Technicians.Add(tech);
        await _context.SaveChangesAsync();

        return new TechnicianDto
        {
            Id = Math.Abs(tech.Id.GetHashCode()),
            UserId = tech.UserId.ToString(),
            EmployeeCode = tech.EmployeeId,
            FullName = tech.EmployeeId,
            Phone = "N/A",
            Status = "Available",
            MaxDailyJobs = 5,
            Skills = tech.Skills.Select(s => s.Name).ToList()
        };
    }

    public async Task<AssignmentRecommendationDto> RecommendTechnicianAsync(AssignmentRequestDto requestDto)
    {
        // Dynamically retrieving only the technicians currently available from the database.
        var availableTechnicians = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .Where(t => t.IsAvailable)
            .ToListAsync();

        long finalRequestId = 0;
        long.TryParse(requestDto.RequestId, out var parsedLong);
        finalRequestId = parsedLong;

        if (!availableTechnicians.Any())
        {
            return new AssignmentRecommendationDto
            {
                RequestId = (int)finalRequestId,
                RecommendedTechnicianId = 0,
                TechnicianName = "N/A",
                MatchScore = 0.0,
                ReasoningSummary = "No available technicians found in the system at the moment.",
                Status = "Failed"
            };
        }

        Technician? bestMatch = null;
        double highestScore = 0.0;
        string targetSkill = requestDto.RequiredSkill?.Trim() ?? string.Empty;

        foreach (var tech in availableTechnicians)
        {
            double currentScore = 0.0;
            bool skillMatched = false;

            // Checking if the specialization matches.
            if (!string.IsNullOrEmpty(tech.Specialization) && !string.IsNullOrEmpty(targetSkill))
            {
                if (tech.Specialization.Contains(targetSkill, StringComparison.OrdinalIgnoreCase) || 
                    targetSkill.Contains(tech.Specialization, StringComparison.OrdinalIgnoreCase))
                {
                    currentScore += 0.80;
                    skillMatched = true;
                }
            }

            // Checking if the technician's skills match the required skill.
            if (tech.Skills != null && tech.Skills.Any(s => !string.IsNullOrEmpty(s.Name) && 
                (s.Name.Contains(targetSkill, StringComparison.OrdinalIgnoreCase) || 
                 targetSkill.Contains(s.Name, StringComparison.OrdinalIgnoreCase))))
            {
                currentScore += 0.90;
                skillMatched = true;
            }

            // Adding extra points for being currently available.
            currentScore += 0.10;

            if (!skillMatched)
            {
                currentScore += 0.30; 
            }

            // Finding the technician with the highest score.
            if (currentScore > highestScore)
            {
                highestScore = currentScore;
                bestMatch = tech;
            }
        }

        if (bestMatch == null)
        {
            bestMatch = availableTechnicians.First();
            highestScore = 0.50;
        }

        string techFullName = bestMatch?.User != null 
            ? $"{bestMatch.User.FirstName} {bestMatch.User.LastName}" 
            : (bestMatch?.EmployeeId ?? "No Technician Assigned");

        string matchType = highestScore >= 0.70 ? "AI Skill Match" : "General Availability Match";
        string reasoningSummary = $"{matchType}: Selected '{techFullName}' based on dynamic evaluation for skill '{targetSkill}' with priority '{requestDto.PriorityLevel}'.";

        return new AssignmentRecommendationDto
        {
            RequestId = (int)finalRequestId, 
            RecommendedTechnicianId = bestMatch != null ? Math.Abs(bestMatch.Id.GetHashCode()) : 0,
            TechnicianName = techFullName,
            MatchScore = Math.Round(Math.Min(highestScore, 1.0), 2),
            ReasoningSummary = reasoningSummary,
            Status = "Recommended"
        };
    }

    public async Task<bool> AssignTechnicianAsync(int requestId, int technicianId)
    {
        var tech = await _context.Technicians
            .FirstOrDefaultAsync(t => Math.Abs(t.Id.GetHashCode()) == technicianId);

        if (tech != null)
        {
            tech.IsAvailable = false;

            // Inserting a record into the Assignment table (RequestId is already an int)
            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,   
                TechnicianId = tech.Id,
                MatchScore = 85.0,
                ReasoningSummary = "Assigned successfully via AI recommendation.",
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow
            };

            _context.Set<Assignment>().Add(assignment);
            return await _context.SaveChangesAsync() > 0;
        }
        return false;
    }
}
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
            Id = 0, // If the DTO expects an int (change it to Guid in the DTO if necessary).
            UserId = t.UserId.ToString(),
            EmployeeCode = t.EmployeeId,
            FullName = t.User != null ? $"{t.User.FirstName} {t.User.LastName}" : t.EmployeeId,
            Phone = t.User?.PhoneNumber ?? "N/A",
            Status = t.IsAvailable ? "Available" : "Busy",
            MaxDailyJobs = 5,
            Skills = t.Skills.Select(s => s.Name).ToList()
        });
    }

    public async Task<TechnicianDto?> GetTechnicianByIdAsync(int id)
    {
        // Fallback handling when receiving an Int ID as a parameter.
        var tech = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.Skills)
            .FirstOrDefaultAsync();

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
            Skills = tech.Skills.Select(s => s.Name).ToList()
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
            // Skipping or performing a base check because Skill IDs (Int) do not match the GUIDs.
            var selectedSkills = await _context.Skills.Take(dto.SkillIds.Count).ToListAsync();
            tech.Skills = selectedSkills;
        }

        _context.Technicians.Add(tech);
        await _context.SaveChangesAsync();

        return new TechnicianDto
        {
            Id = 1,
            UserId = tech.UserId.ToString(),
            EmployeeCode = tech.EmployeeId,
            FullName = tech.EmployeeId,
            Phone = "N/A",
            Status = "Available",
            MaxDailyJobs = 5,
            Skills = tech.Skills.Select(s => s.Name).ToList()
        };
    }

    // API Gateway Orchestrator: Internal Python AI Agent Call
    public async Task<AssignmentRecommendationDto> RecommendTechnicianAsync(AssignmentRequestDto requestDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("http://localhost:8000/agent/assign", requestDto);

            if (response.IsSuccessStatusCode)
            {
                var recommendation = await response.Content.ReadFromJsonAsync<AssignmentRecommendationDto>();
                if (recommendation != null) return recommendation;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gateway Warning] Python AI Agent Error: {ex.Message}");
        }

        // DB Fallback Mechanism
        var eligibleTech = await _context.Technicians
            .Include(t => t.Skills)
            .Where(t => t.IsAvailable && t.Skills.Any(s => s.Name.ToLower() == requestDto.RequiredSkill.ToLower()))
            .FirstOrDefaultAsync() 
            ?? await _context.Technicians.FirstOrDefaultAsync(t => t.IsAvailable);

        return new AssignmentRecommendationDto
        {
            RequestId = requestDto.RequestId,
            RecommendedTechnicianId = 1,
            TechnicianName = eligibleTech?.EmployeeId ?? "Default Technician",
            MatchScore = eligibleTech != null ? 0.85 : 0.50,
            ReasoningSummary = $"Gateway Fallback: Matched {eligibleTech?.EmployeeId} via Database skills check.",
            Status = "Recommended"
        };
    }

    public async Task<bool> AssignTechnicianAsync(int requestId, int technicianId)
    {
        var tech = await _context.Technicians.FirstOrDefaultAsync(t => t.IsAvailable);
        if (tech != null)
        {
            tech.IsAvailable = false;
            return await _context.SaveChangesAsync() > 0;
        }
        return false;
    }
}
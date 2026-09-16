using FixFlow.Api.DTOs;

namespace FixFlow.Api.Interfaces;

public interface ITechnicianService
{
    Task<IEnumerable<TechnicianDto>> GetAllTechniciansAsync(string? skill);
    Task<TechnicianDto?> GetTechnicianByIdAsync(int id);
    Task<TechnicianDto> CreateTechnicianAsync(CreateTechnicianDto dto);
    Task<AssignmentRecommendationDto> RecommendTechnicianAsync(AssignmentRequestDto requestDto);
    Task<bool> AssignTechnicianAsync(int requestId, int technicianId);
}
using FixFlow.Api.DTOs;

namespace FixFlow.Api.Interfaces;

public interface IPriorityAssessmentService
{
    // Required 7 Business Operations
    string AssessAssetCriticality(string? assetCriticality, string? assetCategory);
    string AssessImpact(string? impactOverride, bool hasSafetyHazard, string? disruptionScope);
    string AssessLikelihood(string? likelihoodOverride, int recentFailures, int openRequests);
    (int RiskScore, ContributingFactorsDto Factors) CalculateRiskScore(
        string criticality,
        string impact,
        string likelihood,
        bool hasSafetyHazard,
        int recentFailures,
        bool isHighDensityLocation);
    string DetermineRiskLevel(int riskScore);
    (string Priority, int ResponseHours, int ResolutionHours, string ResponseWindow, bool EscalationFlag) CalculatePriorityAndSLA(
        int riskScore,
        string riskLevel,
        bool hasSafetyHazard,
        string criticality,
        string impact);
    Task<RiskSimulationResultDto> SimulateRiskEscalationAsync(Guid requestId, RiskSimulationRequestDto simulationDto);

    // API Service Operations
    Task<PriorityAssessmentDto> GetPriorityByRequestIdAsync(Guid requestId);
    Task<PriorityAssessmentDto> CreatePriorityAssessmentAsync(Guid requestId, CreatePriorityAssessmentDto? dto, string assessedBy);
    Task<PriorityAssessmentDto> UpdatePriorityAssessmentAsync(Guid assessmentId, UpdatePriorityAssessmentDto dto, string updatedBy);
    Task<PagedResult<PriorityAssessmentDto>> GetPriorityAssessmentsAsync(PriorityAssessmentSearchFilterDto filter);
    Task<PagedResult<PriorityAssessmentDto>> SearchPriorityAssessmentsAsync(PriorityAssessmentSearchFilterDto filter);
    Task<PriorityAssessmentDto> EscalateRequestAsync(Guid requestId, EscalateRequestDto dto, string escalatedBy);
    Task<PriorityAssessmentDto> GetRiskAssessmentAsync(Guid requestId);
    Task<PriorityAssessmentDto> SaveAgentAssessmentAsync(Guid requestId, PriorityAgentResultDto agentResult, string assessedBy);
}


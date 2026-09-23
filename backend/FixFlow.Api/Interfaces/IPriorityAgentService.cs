using FixFlow.Api.DTOs;

namespace FixFlow.Api.Interfaces;

public interface IPriorityAgentService
{
    Task<PriorityAssessmentDto> EvaluateAndPersistAsync(Guid requestId, PriorityAgentEvaluationRequestDto? overrideDto = null, string assessedBy = "PriorityAgent");
    Task<PriorityAgentResultDto> EvaluateAgentAsync(Guid requestId, PriorityAgentEvaluationRequestDto? overrideDto = null);
}

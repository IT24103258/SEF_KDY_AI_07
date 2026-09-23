using FixFlow.Api.DTOs;
using FluentValidation;

namespace FixFlow.Api.Validators;

public class CreatePriorityAssessmentDtoValidator : AbstractValidator<CreatePriorityAssessmentDto>
{
    private static readonly string[] ValidLevels = { "Low", "Medium", "High", "Critical" };

    public CreatePriorityAssessmentDtoValidator()
    {
        RuleFor(x => x.AssetCriticalityOverride)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("AssetCriticalityOverride must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.ImpactOverride)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("ImpactOverride must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.LikelihoodOverride)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("LikelihoodOverride must be one of: Low, Medium, High, Critical.");
    }
}

public class UpdatePriorityAssessmentDtoValidator : AbstractValidator<UpdatePriorityAssessmentDto>
{
    private static readonly string[] ValidLevels = { "Low", "Medium", "High", "Critical" };

    public UpdatePriorityAssessmentDtoValidator()
    {
        RuleFor(x => x.Priority)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("Priority must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.RiskLevel)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("RiskLevel must be one of: Low, Medium, High, Critical.");
    }
}

public class EscalateRequestDtoValidator : AbstractValidator<EscalateRequestDto>
{
    public EscalateRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Escalation reason is required.")
            .MaximumLength(500).WithMessage("Escalation reason cannot exceed 500 characters.");
    }
}

public class RiskSimulationRequestDtoValidator : AbstractValidator<RiskSimulationRequestDto>
{
    private static readonly string[] ValidLevels = { "Low", "Medium", "High", "Critical" };

    public RiskSimulationRequestDtoValidator()
    {
        RuleFor(x => x.AssetCriticality)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("AssetCriticality must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.ImpactLevel)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("ImpactLevel must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.LikelihoodLevel)
            .Must(x => string.IsNullOrEmpty(x) || ValidLevels.Contains(x))
            .WithMessage("LikelihoodLevel must be one of: Low, Medium, High, Critical.");

        RuleFor(x => x.RecentFailureCount)
            .GreaterThanOrEqualTo(0).When(x => x.RecentFailureCount.HasValue)
            .WithMessage("RecentFailureCount cannot be negative.");
    }
}

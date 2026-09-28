using FixFlow.Api.DTOs;
using FluentValidation;

namespace FixFlow.Api.Validators;

/// <summary>
/// Validates POST /api/requests body.
/// Auto-registered by AddValidatorsFromAssemblyContaining&lt;Program&gt;() in Program.cs.
/// </summary>
public class CreateRequestDtoValidator : AbstractValidator<CreateRequestDto>
{
    public CreateRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters so the AI can classify it accurately.");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("A location must be selected.");
    }
}

/// <summary>
/// Validates PUT /api/requests/{id} body.
/// All fields are optional in an update — only the supplied (non-null) ones are validated.
/// </summary>
public class UpdateRequestDtoValidator : AbstractValidator<UpdateRequestDto>
{
    public UpdateRequestDtoValidator()
    {
        // Only validate when the client explicitly sends a value
        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title!)
                .NotEmpty().WithMessage("Title cannot be an empty string.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description!)
                .NotEmpty().WithMessage("Description cannot be an empty string.")
                .MinimumLength(20).WithMessage("Description must be at least 20 characters.");
        });
    }
}

/// <summary>
/// Validates PUT /api/requests/{id}/classification (manager override) body.
/// </summary>
public class ClassificationOverrideDtoValidator : AbstractValidator<ClassificationOverrideDto>
{
    public ClassificationOverrideDtoValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required for an override.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        When(x => x.Subcategory != null, () =>
        {
            RuleFor(x => x.Subcategory!)
                .MaximumLength(100).WithMessage("Subcategory must not exceed 100 characters.");
        });

        When(x => x.RequiredSkill != null, () =>
        {
            RuleFor(x => x.RequiredSkill!)
                .MaximumLength(100).WithMessage("Required skill must not exceed 100 characters.");
        });

        When(x => x.Reason != null, () =>
        {
            RuleFor(x => x.Reason!)
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
        });
    }
}

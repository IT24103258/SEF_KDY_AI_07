using FixFlow.Api.DTOs;
using FluentValidation;

namespace FixFlow.Api.Validators;

public class WorkOrderCreateDtoValidator : AbstractValidator<WorkOrderCreateDto>
{
    public WorkOrderCreateDtoValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required.");

        RuleFor(x => x.TechnicianId)
            .NotEmpty().WithMessage("Technician ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Work order title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("Estimated duration must be greater than 0 minutes.")
            .LessThanOrEqualTo(1440).WithMessage("Estimated duration cannot exceed 24 hours (1440 minutes).");

        RuleFor(x => x)
            .Must(x => !x.ScheduledStartTime.HasValue || !x.ScheduledEndTime.HasValue || x.ScheduledStartTime.Value < x.ScheduledEndTime.Value)
            .WithMessage("Scheduled start time must be earlier than scheduled end time.");
    }
}

public class WorkOrderUpdateDtoValidator : AbstractValidator<WorkOrderUpdateDto>
{
    public WorkOrderUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Work order title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("Estimated duration must be greater than 0 minutes.")
            .LessThanOrEqualTo(1440).WithMessage("Estimated duration cannot exceed 24 hours.");

        RuleFor(x => x)
            .Must(x => !x.ScheduledStartTime.HasValue || !x.ScheduledEndTime.HasValue || x.ScheduledStartTime.Value < x.ScheduledEndTime.Value)
            .WithMessage("Scheduled start time must be earlier than scheduled end time.");
    }
}

public class ScheduleRequestDtoValidator : AbstractValidator<ScheduleRequestDto>
{
    public ScheduleRequestDtoValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required.");

        RuleFor(x => x.TechnicianId)
            .NotEmpty().WithMessage("Technician ID is required.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("Estimated duration must be greater than 0 minutes.");
    }
}

public class WorkOrderStatusUpdateDtoValidator : AbstractValidator<WorkOrderStatusUpdateDto>
{
    public WorkOrderStatusUpdateDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Target status is required.");
    }
}

public class CompleteWorkOrderDtoValidator : AbstractValidator<CompleteWorkOrderDto>
{
    public CompleteWorkOrderDtoValidator()
    {
        RuleFor(x => x.SignerName)
            .NotEmpty().WithMessage("Customer / Signer name is required for job sign-off.");
    }
}

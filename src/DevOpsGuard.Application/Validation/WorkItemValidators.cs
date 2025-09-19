using DevOpsGuard.Application.DTOs;
using FluentValidation;

namespace DevOpsGuard.Application.Validation;

public sealed class WorkItemCreateRequestValidator : AbstractValidator<WorkItemCreateRequest>
{
    public WorkItemCreateRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Service)
            .NotEmpty().WithMessage("Service is required.")
            .MaximumLength(100);

        RuleFor(x => x.Component)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Component));

        RuleFor(x => x.Assignee)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Assignee));

        RuleForEach(x => x.Labels!)
            .MaximumLength(30).WithMessage("Each label must be <= 30 chars.")
            .When(x => x.Labels is not null);

        RuleFor(x => x.Labels)
            .Must(l => l == null || l.Count <= 20)
            .WithMessage("Too many labels (max 20).");
    }
}

public sealed class WorkItemUpdateRequestValidator : AbstractValidator<WorkItemUpdateRequest>
{
    public WorkItemUpdateRequestValidator()
    {
        // Only validate fields that are present
        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title!)
                .NotEmpty().WithMessage("Title cannot be empty.")
                .MaximumLength(200);
        });

        When(x => x.Service is not null, () =>
        {
            RuleFor(x => x.Service!)
                .NotEmpty().WithMessage("Service cannot be empty.")
                .MaximumLength(100);
        });

        When(x => x.Component is not null, () =>
        {
            RuleFor(x => x.Component!)
                .MaximumLength(100);
        });

        When(x => x.Assignee is not null, () =>
        {
            RuleFor(x => x.Assignee!)
                .MaximumLength(100);
        });

        When(x => x.Labels is not null, () =>
        {
            RuleForEach(x => x.Labels!)
                .MaximumLength(30).WithMessage("Each label must be <= 30 chars.");
            RuleFor(x => x.Labels!)
                .Must(l => l.Count <= 20)
                .WithMessage("Too many labels (max 20).");
        });
    }
}

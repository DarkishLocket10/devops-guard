using DevOpsGuard.Application.DTOs;
using FluentValidation;

namespace DevOpsGuard.Application.Validation;

public sealed class EventIngestRequestValidator : AbstractValidator<EventIngestRequest>
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "build_failed", "incident_opened", "deploy_succeeded", "coverage_dropped"
    };

    public EventIngestRequestValidator()
    {
        RuleFor(x => x.WorkItemId).NotEmpty();
        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(k => Allowed.Contains(k))
            .WithMessage("Kind must be one of: build_failed, incident_opened, deploy_succeeded, coverage_dropped.");

        RuleFor(x => x.OccurredAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("OccurredAtUtc cannot be in the far future.");
    }
}

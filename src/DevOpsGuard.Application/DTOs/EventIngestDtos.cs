namespace DevOpsGuard.Application.DTOs;

public sealed record EventIngestRequest(
    Guid WorkItemId,
    string Kind,           // "build_failed" | "incident_opened" | "deploy_succeeded" | "coverage_dropped"
    string? Source,        // e.g., "github-actions", "pagerduty"
    string? Message,       // free text
    DateTime OccurredAtUtc // when it happened
);

public sealed record EventIngestResponse(
    Guid WorkItemId,
    string AppliedRule);   // e.g., "raised_to_high_and_in_progress"

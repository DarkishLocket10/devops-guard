using DevOpsGuard.Domain.Enums;

namespace DevOpsGuard.Application.DTOs; // Data Transfer Objects (DTOs) for WorkItem

// here, we define request/response models for creating, updating, and retrieving WorkItems
// these are simple records that map closely to the WorkItem entity, but are decoupled from it for flexibility and security reasons (e.g., not exposing internal IDs or timestamps unnecessarily)

public sealed record WorkItemCreateRequest(
    string Title,
    string Service,
    Priority Priority,
    DateOnly? DueDate,
    string? Component,
    string? Assignee,
    List<string>? Labels);

public sealed record WorkItemUpdateRequest(
    string? Title,
    string? Service,
    Priority? Priority,
    DateOnly? DueDate,
    string? Component,
    string? Assignee,
    List<string>? Labels,
    WorkItemStatus? Status);

public sealed record WorkItemResponse(
    Guid Id,
    string Title,
    string Service,
    Priority Priority,
    DateOnly? DueDate,
    WorkItemStatus Status,
    string? Component,
    string? Assignee,
    List<string> Labels,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

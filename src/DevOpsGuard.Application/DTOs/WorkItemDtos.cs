using DevOpsGuard.Domain.Enums;

namespace DevOpsGuard.Application.DTOs;

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

using DevOpsGuard.Domain.Enums;

namespace DevOpsGuard.Domain.Entities;

public sealed class WorkItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; private set; }
    public string Service { get; private set; }
    public Priority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public WorkItemStatus Status { get; private set; } = WorkItemStatus.Open;
    public string? Component { get; private set; }
    public string? Assignee { get; private set; }
    public List<string> Labels { get; private set; } = new();

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public WorkItem(string title, string service, Priority priority, DateOnly? dueDate = null)
    {
        Title = !string.IsNullOrWhiteSpace(title)
            ? title.Trim()
            : throw new ArgumentException("Title is required.", nameof(title));

        Service = !string.IsNullOrWhiteSpace(service)
            ? service.Trim()
            : throw new ArgumentException("Service is required.", nameof(service));

        Priority = priority;
        DueDate = dueDate;
    }

    // -------- Domain behaviors (no reflection needed) --------
    public void Rename(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty.", nameof(newTitle));

        Title = newTitle.Trim();
        Touch();
    }

    public void MoveToService(string newService)
    {
        if (string.IsNullOrWhiteSpace(newService))
            throw new ArgumentException("Service cannot be empty.", nameof(newService));

        Service = newService.Trim();
        Touch();
    }

    public void ChangePriority(Priority priority)
    {
        Priority = priority;
        Touch();
    }

    public void SetStatus(WorkItemStatus status)
    {
        Status = status;
        Touch();
    }

    public void SetDueDate(DateOnly? dueDate)
    {
        DueDate = dueDate;
        Touch();
    }

    public void AssignTo(string? assignee)
    {
        Assignee = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
        Touch();
    }

    public void SetComponent(string? component)
    {
        Component = string.IsNullOrWhiteSpace(component) ? null : component.Trim();
        Touch();
    }

    public void ReplaceLabels(IEnumerable<string>? labels)
    {
        Labels.Clear();
        if (labels is not null)
        {
            foreach (var l in labels.Where(l => !string.IsNullOrWhiteSpace(l)))
                Labels.Add(l.Trim());
        }
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

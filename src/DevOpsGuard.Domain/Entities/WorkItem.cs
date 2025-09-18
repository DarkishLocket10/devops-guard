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
    public List<string> Labels { get; } = new();

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    // ctor enforces minimal required fields
    public WorkItem(string title, string service, Priority priority, DateOnly? dueDate = null)
    {
        Title = !string.IsNullOrWhiteSpace(title)
            ? title
            : throw new ArgumentException("Title is required.", nameof(title));

        Service = !string.IsNullOrWhiteSpace(service)
            ? service
            : throw new ArgumentException("Service is required.", nameof(service));

        Priority = priority;
        DueDate = dueDate;
    }

    // domain behaviors (tiny for now)
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

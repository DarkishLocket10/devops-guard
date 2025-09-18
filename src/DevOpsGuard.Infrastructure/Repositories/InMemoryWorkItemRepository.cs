using DevOpsGuard.Application.Abstractions;
using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Domain.Enums;

namespace DevOpsGuard.Infrastructure.Repositories;

public sealed class InMemoryWorkItemRepository : IWorkItemRepository
{
    // simple in-memory store
    private readonly List<WorkItem> _items = new();
    private readonly object _lock = new();

    public Task<WorkItem> AddAsync(WorkItem item, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _items.Add(item);
        }
        return Task.FromResult(item);
    }

    public Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
        }
    }

    public Task<(IReadOnlyList<WorkItem> Items, int TotalCount)> ListAsync(
        string? service = null,
        WorkItemStatus? status = null,
        string? assignee = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        IEnumerable<WorkItem> query;
        lock (_lock)
        {
            query = _items.ToList(); // copy for thread-safety
        }

        if (!string.IsNullOrWhiteSpace(service))
            query = query.Where(i => string.Equals(i.Service, service, StringComparison.OrdinalIgnoreCase));

        if (status is not null)
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(assignee))
            query = query.Where(i => string.Equals(i.Assignee, assignee, StringComparison.OrdinalIgnoreCase));

        // simple sort: newest updated first
        query = query.OrderByDescending(i => i.UpdatedAtUtc);

        var total = query.Count();
        var items = query.Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
                         .Take(Math.Max(pageSize, 1))
                         .ToList();

        return Task.FromResult(((IReadOnlyList<WorkItem>)items, total));
    }

    public Task UpdateAsync(WorkItem item, CancellationToken ct = default)
    {
        // nothing to do for in-memory; the instance is already modified
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _items.RemoveAll(i => i.Id == id);
        }
        return Task.CompletedTask;
    }
}

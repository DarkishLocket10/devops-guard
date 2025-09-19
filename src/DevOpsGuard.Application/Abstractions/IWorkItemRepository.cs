using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Domain.Enums;

namespace DevOpsGuard.Application.Abstractions;

public interface IWorkItemRepository
{
    Task<WorkItem> AddAsync(WorkItem item, CancellationToken ct = default); // returns the added item (with Id populated)
    Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default); // returns null if not found

    // Basic list with filters & paging
    Task<(IReadOnlyList<WorkItem> Items, int TotalCount)> ListAsync(
        string? service = null,
        WorkItemStatus? status = null,
        string? assignee = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,     // "updatedAt", "priority", "dueDate"
        string? sortDir = null,    // "asc" or "desc"
        CancellationToken ct = default);


    Task UpdateAsync(WorkItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

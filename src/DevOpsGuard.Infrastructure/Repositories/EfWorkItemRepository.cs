using DevOpsGuard.Application.Abstractions;
using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Domain.Enums;
using DevOpsGuard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevOpsGuard.Infrastructure.Repositories;

public sealed class EfWorkItemRepository : IWorkItemRepository
{
    private readonly DevOpsGuardDbContext _db;

    public EfWorkItemRepository(DevOpsGuardDbContext db)
    {
        _db = db;
    }

    public async Task<WorkItem> AddAsync(WorkItem item, CancellationToken ct = default)
    {
        await _db.WorkItems.AddAsync(item, ct);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.WorkItems.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

public async Task<(IReadOnlyList<WorkItem> Items, int TotalCount)> ListAsync(
    string? service = null,
    WorkItemStatus? status = null,
    string? assignee = null,
    int page = 1,
    int pageSize = 20,
    string? sortBy = null,
    string? sortDir = null,
    CancellationToken ct = default)
{
    var query = _db.WorkItems.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(service))
        query = query.Where(i => i.Service == service);
    if (status is not null)
        query = query.Where(i => i.Status == status);
    if (!string.IsNullOrWhiteSpace(assignee))
        query = query.Where(i => i.Assignee == assignee);

    var by = (sortBy ?? "updatedAt").ToLowerInvariant();
    var dir = (sortDir ?? "desc").ToLowerInvariant();

    // EF needs strongly-typed OrderBy
    query = (by, dir) switch
    {
        ("priority", "asc")  => query.OrderBy(i => i.Priority).ThenByDescending(i => i.UpdatedAtUtc),
        ("priority", _)      => query.OrderByDescending(i => i.Priority).ThenByDescending(i => i.UpdatedAtUtc),

        ("duedate", "asc")   => query.OrderBy(i => i.DueDate).ThenByDescending(i => i.UpdatedAtUtc),
        ("duedate", _)       => query.OrderByDescending(i => i.DueDate).ThenByDescending(i => i.UpdatedAtUtc),

        // default: updatedAt
        ("updatedat", "asc") => query.OrderBy(i => i.UpdatedAtUtc),
        _                    => query.OrderByDescending(i => i.UpdatedAtUtc)
    };

    var total = await query.CountAsync(ct);
    var items = await query
        .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
        .Take(Math.Max(pageSize, 1))
        .ToListAsync(ct);

    return (items, total);
}


    public async Task UpdateAsync(WorkItem item, CancellationToken ct = default)
    {
        // item is usually tracked if fetched via this repo; if not, attach then mark modified
        _db.WorkItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.WorkItems.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entity is null) return;

        _db.WorkItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}

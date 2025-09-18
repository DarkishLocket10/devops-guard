using DevOpsGuard.Application.Abstractions;
using DevOpsGuard.Application.DTOs;
using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Domain.Enums;
using DevOpsGuard.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register in-memory repo (later we'll swap to EF Core)
builder.Services.AddSingleton<IWorkItemRepository, InMemoryWorkItemRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Root & health
app.MapGet("/", () => Results.Ok(new
{
    name = "DevOps Guard API",
    version = "0.1.0",
    message = "Welcome! Swagger is at /swagger, health at /health."
}));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// -------------------------
// WorkItems (v1 minimal)
// -------------------------

// Create
app.MapPost("/workitems", async Task<Results<Created<WorkItemResponse>, BadRequest<string>>> (
    WorkItemCreateRequest req,
    IWorkItemRepository repo,
    CancellationToken ct) =>
{
    // very light validation
    if (string.IsNullOrWhiteSpace(req.Title)) return TypedResults.BadRequest("Title is required.");
    if (string.IsNullOrWhiteSpace(req.Service)) return TypedResults.BadRequest("Service is required.");

    var entity = new WorkItem(req.Title, req.Service, req.Priority, req.DueDate);
    entity.SetComponent(req.Component);
    entity.AssignTo(req.Assignee);
    if (req.Labels is not null) entity.ReplaceLabels(req.Labels);

    await repo.AddAsync(entity, ct);

    var dto = ToResponse(entity);
    return TypedResults.Created($"/workitems/{dto.Id}", dto);
})
.WithName("CreateWorkItem")
.Produces<WorkItemResponse>(201)
.Produces<string>(400);

// Get by id
app.MapGet("/workitems/{id:guid}", async Task<Results<Ok<WorkItemResponse>, NotFound>> (
    Guid id,
    IWorkItemRepository repo,
    CancellationToken ct) =>
{
    var entity = await repo.GetByIdAsync(id, ct);
    if (entity is null) return TypedResults.NotFound();
    return TypedResults.Ok(ToResponse(entity));
})
.WithName("GetWorkItem")
.Produces<WorkItemResponse>(200)
.Produces(404);

// List with simple filters + paging
app.MapGet("/workitems", async Task<Results<Ok<WorkItemListResponse>, BadRequest<string>>> (
    string? service,
    WorkItemStatus? status,
    string? assignee,
    int page = 1,
    int pageSize = 20,
    IWorkItemRepository repo = null!,
    CancellationToken ct = default) =>
{
    // simple validation: page >= 1; pageSize in [1, 100]
    if (page < 1) return TypedResults.BadRequest("page must be >= 1.");
    if (pageSize < 1 || pageSize > 100) return TypedResults.BadRequest("pageSize must be between 1 and 100.");

    var (items, total) = await repo.ListAsync(service, status, assignee, page, pageSize, ct);

    var response = new WorkItemListResponse(
        Page: page,
        PageSize: pageSize,
        Total: total,
        Items: items.Select(ToResponse).ToList()
    );

    return TypedResults.Ok(response);
})
.WithName("ListWorkItems")
.Produces<WorkItemListResponse>(200)
.Produces<string>(400);



// Update (partial)
app.MapPatch("/workitems/{id:guid}", async Task<Results<Ok<WorkItemResponse>, NotFound, BadRequest<string>>> (
    Guid id,
    WorkItemUpdateRequest req,
    IWorkItemRepository repo,
    CancellationToken ct) =>
{
    var entity = await repo.GetByIdAsync(id, ct);
    if (entity is null) return TypedResults.NotFound();

    if (req.Title is not null && string.IsNullOrWhiteSpace(req.Title))
        return TypedResults.BadRequest("Title cannot be empty.");
    if (req.Service is not null && string.IsNullOrWhiteSpace(req.Service))
        return TypedResults.BadRequest("Service cannot be empty.");

    if (req.Title is not null) entity.Rename(req.Title);
    if (req.Service is not null) entity.MoveToService(req.Service);
    if (req.Priority is not null) entity.ChangePriority(req.Priority.Value);
    if (req.DueDate is not null) entity.SetDueDate(req.DueDate);
    if (req.Component is not null) entity.SetComponent(req.Component);
    if (req.Assignee is not null) entity.AssignTo(req.Assignee);
    if (req.Labels is not null) entity.ReplaceLabels(req.Labels);
    if (req.Status is not null) entity.SetStatus(req.Status.Value);

    await repo.UpdateAsync(entity, ct);
    return TypedResults.Ok(ToResponse(entity));
})
.WithName("UpdateWorkItem")
.Produces<WorkItemResponse>(200)
.Produces<string>(400)
.Produces(404);


// Delete
app.MapDelete("/workitems/{id:guid}", async Task<Results<NoContent, NotFound>> (
    Guid id,
    IWorkItemRepository repo,
    CancellationToken ct) =>
{
    var existing = await repo.GetByIdAsync(id, ct);
    if (existing is null) return TypedResults.NotFound();

    await repo.DeleteAsync(id, ct);
    return TypedResults.NoContent();
})
.WithName("DeleteWorkItem")
.Produces(204)
.Produces(404);


// Dev-only seed endpoint (idempotent-ish within one process run)
app.MapPost("/dev/seed", async Task<IResult> (
    IWorkItemRepository repo,
    CancellationToken ct) =>
{
    var items = new[]
    {
        new WorkItemCreateRequest("Fix NRE in billing webhook", "billing", Priority.High,  DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),  "billing-api", "alex",  new(){"bug","payments"}),
        new WorkItemCreateRequest("Add rate limiting to gateway", "api-gateway", Priority.P0, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),  "gateway",     "jamie", new(){"security","p0"}),
        new WorkItemCreateRequest("Improve docs sidebar",        "docs",     Priority.Medium, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),  "docs-site",   "riley", new(){"ux"}),
        new WorkItemCreateRequest("Cache hot path in web",       "web",      Priority.Low,    null,                                                "frontend",    null,    new(){"perf"}),
        new WorkItemCreateRequest("Migrate to new auth lib",     "api-gateway", Priority.High, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),"auth",       "alex",  new(){"tech-debt"})
    };

    foreach (var req in items)
    {
        var e = new WorkItem(req.Title, req.Service, req.Priority, req.DueDate);
        e.SetComponent(req.Component);
        e.AssignTo(req.Assignee);
        if (req.Labels is not null) e.ReplaceLabels(req.Labels);
        await repo.AddAsync(e, ct);
    }

    return TypedResults.Ok(new { seeded = items.Length });
})
.WithName("SeedDemoData")
.Produces(200);




// Helpers for mapping & tiny updates
static WorkItemResponse ToResponse(WorkItem e) =>
    new(e.Id, e.Title, e.Service, e.Priority, e.DueDate, e.Status, e.Component, e.Assignee, e.Labels.ToList(), e.CreatedAtUtc, e.UpdatedAtUtc);

app.Run();

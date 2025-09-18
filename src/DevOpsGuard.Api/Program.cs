using DevOpsGuard.Application.Abstractions;
using DevOpsGuard.Application.DTOs;
using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Domain.Enums;
using DevOpsGuard.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

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
app.MapGet("/workitems", async Task<IResult> (
    string? service,
    WorkItemStatus? status,
    string? assignee,
    int page = 1,
    int pageSize = 20,
    IWorkItemRepository repo = null!,
    CancellationToken ct = default) =>
{
    var (items, total) = await repo.ListAsync(service, status, assignee, page, pageSize, ct);
    var result = new
    {
        page,
        pageSize,
        total,
        items = items.Select(ToResponse).ToList()
    };
    return TypedResults.Ok(result);
})
.WithName("ListWorkItems")
.Produces(200);


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

// Helpers for mapping & tiny updates
static WorkItemResponse ToResponse(WorkItem e) =>
    new(e.Id, e.Title, e.Service, e.Priority, e.DueDate, e.Status, e.Component, e.Assignee, e.Labels.ToList(), e.CreatedAtUtc, e.UpdatedAtUtc);

app.Run();

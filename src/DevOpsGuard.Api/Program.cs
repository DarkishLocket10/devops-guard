using DevOpsGuard.Application.Abstractions;
using DevOpsGuard.Application.DTOs;
using DevOpsGuard.Domain.Entities;
using DevOpsGuard.Domain.Enums;
using DevOpsGuard.Infrastructure.Repositories;
using DevOpsGuard.Application.Validation;
using DevOpsGuard.Api.Filters;
using DevOpsGuard.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OpenApi;
using System.Globalization;
using System.Text.Json.Serialization;
using FluentValidation;





var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


// Services
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Declare an API Key scheme called "ApiKeyAuth"
    options.AddSecurityDefinition("ApiKeyAuth", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste your API key here"
    });

    // Require it by default (Swagger UI will show an 'Authorize' button)
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKeyAuth"
                }
            },
            Array.Empty<string>()
        }
    });
});


var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<DevOpsGuardDbContext>(options =>
    options.UseSqlServer(cs));

// ProblemDetails for standardized errors
builder.Services.AddProblemDetails();

// Register all FluentValidation validators from Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<WorkItemCreateRequestValidator>();


// Register in-memory repo (later we'll swap to EF Core)
//builder.Services.AddSingleton<IWorkItemRepository, InMemoryWorkItemRepository>();

// Toggle between in-memory and SQL repositories
var useSql = builder.Configuration.GetValue<bool>("UseSqlServer");

if (useSql)
{
    builder.Services.AddScoped<IWorkItemRepository, EfWorkItemRepository>();
}
else
{
    builder.Services.AddSingleton<IWorkItemRepository, InMemoryWorkItemRepository>();
}

var apiKey = builder.Configuration.GetValue<string>("ApiKey") ?? string.Empty;
var requireApiKey = new ApiKeyFilter(apiKey);


var app = builder.Build();

// Developer exception page is fine in Dev, but also map ProblemDetails for non-dev
app.UseExceptionHandler();
app.UseStatusCodePages(); // optional, surfaces simple pages for non-JSON requests


if (useSql)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DevOpsGuardDbContext>();
    db.Database.Migrate(); // creates DB and applies migrations
}


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
.AddEndpointFilter(new ValidationFilter<WorkItemCreateRequest>())
.WithName("CreateWorkItem")
.Produces<WorkItemResponse>(201)
.Produces<string>(400)
.AddEndpointFilter(requireApiKey)
.WithOpenApi(op =>
{
    op.Summary = "Create a new work item";
    op.Description = "Creates a work item for a given service with optional assignee, component, labels, and due date.";

    // Example request body
    op.RequestBody = op.RequestBody ?? new Microsoft.OpenApi.Models.OpenApiRequestBody();
    op.RequestBody.Content["application/json"].Example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["title"]    = new Microsoft.OpenApi.Any.OpenApiString("Fix NRE in billing webhook"),
        ["service"]  = new Microsoft.OpenApi.Any.OpenApiString("billing"),
        ["priority"] = new Microsoft.OpenApi.Any.OpenApiString("High"),
        ["dueDate"]  = new Microsoft.OpenApi.Any.OpenApiString(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd")),
        ["component"]= new Microsoft.OpenApi.Any.OpenApiString("billing-api"),
        ["assignee"] = new Microsoft.OpenApi.Any.OpenApiString("alex"),
        ["labels"]   = new Microsoft.OpenApi.Any.OpenApiArray
        {
            new Microsoft.OpenApi.Any.OpenApiString("bug"),
            new Microsoft.OpenApi.Any.OpenApiString("payments")
        }
    };
    return op;
});



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
    string? sortBy = "updatedAt",   // new
    string? sortDir = "desc",       // new
    IWorkItemRepository repo = null!,
    CancellationToken ct = default) =>
{
    if (page < 1) return TypedResults.BadRequest("page must be >= 1.");
    if (pageSize < 1 || pageSize > 100) return TypedResults.BadRequest("pageSize must be between 1 and 100.");

    var by = (sortBy ?? "updatedAt").ToLowerInvariant();
    if (by is not ("updatedat" or "priority" or "duedate"))
        return TypedResults.BadRequest("sortBy must be one of: updatedAt, priority, dueDate.");

    var dir = (sortDir ?? "desc").ToLowerInvariant();
    if (dir is not ("asc" or "desc"))
        return TypedResults.BadRequest("sortDir must be 'asc' or 'desc'.");

    var (items, total) = await repo.ListAsync(service, status, assignee, page, pageSize, by, dir, ct);

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
.Produces<string>(400)
.WithOpenApi(op =>
{
    op.Summary = "List work items";
    op.Description = "Returns a paged list with optional filters (service, status, assignee) and sorting (updatedAt, priority, dueDate).";

    // Example 200 response
    var example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["page"] = new Microsoft.OpenApi.Any.OpenApiInteger(1),
        ["pageSize"] = new Microsoft.OpenApi.Any.OpenApiInteger(20),
        ["total"] = new Microsoft.OpenApi.Any.OpenApiInteger(42),
        ["items"] = new Microsoft.OpenApi.Any.OpenApiArray
        {
            new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["id"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["title"] = new Microsoft.OpenApi.Any.OpenApiString("Add rate limiting to gateway"),
                ["service"] = new Microsoft.OpenApi.Any.OpenApiString("api-gateway"),
                ["priority"] = new Microsoft.OpenApi.Any.OpenApiString("P0"),
                ["dueDate"] = new Microsoft.OpenApi.Any.OpenApiString(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd")),
                ["status"] = new Microsoft.OpenApi.Any.OpenApiString("Open"),
                ["component"] = new Microsoft.OpenApi.Any.OpenApiString("gateway"),
                ["assignee"] = new Microsoft.OpenApi.Any.OpenApiString("jamie"),
                ["labels"] = new Microsoft.OpenApi.Any.OpenApiArray
                {
                    new Microsoft.OpenApi.Any.OpenApiString("security"),
                    new Microsoft.OpenApi.Any.OpenApiString("p0")
                },
                ["createdAtUtc"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.AddDays(-2).ToString("O")),
                ["updatedAtUtc"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.ToString("O"))
            }
        }
    };

    if (op.Responses.TryGetValue("200", out var resp) &&
        resp.Content.TryGetValue("application/json", out var media))
    {
        media.Example = example;
    }

    return op;
});




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
.AddEndpointFilter(new ValidationFilter<WorkItemUpdateRequest>())
.WithName("UpdateWorkItem")
.Produces<WorkItemResponse>(200)
.Produces<string>(400)
.AddEndpointFilter(requireApiKey)
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
.AddEndpointFilter(requireApiKey)
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
.AddEndpointFilter(requireApiKey)
.Produces(200);


// -------------------------
// Events ingest (simple rules)
// -------------------------
app.MapPost("/events/ingest", async Task<Results<Ok<EventIngestResponse>, NotFound, BadRequest<string>>> (
    EventIngestRequest req,
    IWorkItemRepository repo,
    CancellationToken ct) =>
{
    var item = await repo.GetByIdAsync(req.WorkItemId, ct);
    if (item is null) return TypedResults.NotFound();

    // normalize kind
    var kind = (req.Kind ?? "").Trim().ToLowerInvariant();
    string applied = "none";

    switch (kind)
    {
        case "build_failed":
            item.ReplaceLabels(item.Labels.Concat(new[] { "build-failed" }).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            if (item.Priority < DevOpsGuard.Domain.Enums.Priority.High)
                item.ChangePriority(DevOpsGuard.Domain.Enums.Priority.High);
            item.SetStatus(DevOpsGuard.Domain.Enums.WorkItemStatus.InProgress);
            applied = "raised_to_high_and_in_progress";
            break;

        case "incident_opened":
            item.ReplaceLabels(item.Labels.Concat(new[] { "incident" }).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            item.ChangePriority(DevOpsGuard.Domain.Enums.Priority.P0);
            item.SetStatus(DevOpsGuard.Domain.Enums.WorkItemStatus.Blocked);
            applied = "p0_and_blocked";
            break;

        case "deploy_succeeded":
            item.ReplaceLabels(item.Labels.Concat(new[] { "deploy-ok" }).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            applied = "noted_deploy_ok";
            break;

        case "coverage_dropped":
            item.ReplaceLabels(item.Labels.Concat(new[] { "qa" }).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            if (item.Priority < DevOpsGuard.Domain.Enums.Priority.Medium)
                item.ChangePriority(DevOpsGuard.Domain.Enums.Priority.Medium);
            applied = "raised_to_minimum_medium";
            break;

        default:
            return TypedResults.BadRequest("Unknown event kind.");
    }

    await repo.UpdateAsync(item, ct);
    return TypedResults.Ok(new EventIngestResponse(item.Id, applied));
})
.AddEndpointFilter(new ValidationFilter<EventIngestRequest>()) // reuse our validation filter
.AddEndpointFilter(requireApiKey)                               // protect if you like
.WithName("IngestEvent")
.Produces<EventIngestResponse>(200)
.Produces<string>(400)
.Produces(404)
.WithOpenApi(op =>
{
    op.Summary = "Ingest an external event (CI/CD, incident)";
    op.Description = "Applies simple rules to the referenced work item (e.g., raises priority, sets status, adds labels).";
    op.RequestBody = op.RequestBody ?? new Microsoft.OpenApi.Models.OpenApiRequestBody();
    op.RequestBody.Content["application/json"].Example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["workItemId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
        ["kind"] = new Microsoft.OpenApi.Any.OpenApiString("build_failed"),
        ["source"] = new Microsoft.OpenApi.Any.OpenApiString("github-actions"),
        ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Build 123 failed on main"),
        ["occurredAtUtc"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.UtcNow.ToString("O"))
    };
    return op;
});



// -------------------------
// Metrics (SQL-backed)
// -------------------------
app.MapGet("/metrics", async Task<Results<Ok<MetricsResponse>, ProblemHttpResult>> (
    DevOpsGuardDbContext db,
    CancellationToken ct) =>
{
    try
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Open items = anything not Done
        var openItemsQ = db.WorkItems
            .AsNoTracking()
            .Where(w => w.Status != WorkItemStatus.Resolved);

        var openCount = await openItemsQ.CountAsync(ct);

        var touchedRecently = await openItemsQ
            .CountAsync(w => w.UpdatedAtUtc >= sevenDaysAgo, ct);

        var overdueOpen = await openItemsQ
            .CountAsync(w => w.DueDate != null && w.DueDate < today, ct);

        var backlogHealthPct = openCount == 0 ? 100.0 : Math.Round(100.0 * touchedRecently / openCount, 1);
        var slaBreachRatePct = openCount == 0 ? 0.0 : Math.Round(100.0 * overdueOpen / openCount, 1);

        // Risk stub: base(priority) + 3 * daysOverdue, clamped to [0,100]
        var comps = await openItemsQ
            .Select(w => new { w.Priority, w.DueDate })
            .ToListAsync(ct);

        double avgRisk = 0.0;
        if (comps.Count > 0)
        {
            double sum = 0;
            foreach (var x in comps)
            {
                var baseScore = x.Priority switch
                {
                    Priority.Low    => 10,
                    Priority.Medium => 25,
                    Priority.High   => 50,
                    Priority.P0     => 70,
                    _               => 25
                };
                var daysOverdue = x.DueDate.HasValue
                    ? Math.Max(0, today.DayNumber - x.DueDate.Value.DayNumber)
                    : 0;
                sum += Math.Clamp(baseScore + 3 * daysOverdue, 0, 100);
            }
            avgRisk = Math.Round(sum / comps.Count, 1);
        }

        return TypedResults.Ok(new MetricsResponse(
            BacklogHealthPct: backlogHealthPct,
            SlaBreachRatePct: slaBreachRatePct,
            OverdueCount: overdueOpen,
            Risk: new RiskSummary(avgRisk)
        ));
    }
    catch (Exception ex)
    {
        return TypedResults.Problem(
            title: "Metrics unavailable",
            detail: $"Metrics require the SQL database. Error: {ex.Message}",
            statusCode: StatusCodes.Status500InternalServerError);
    }
})
.WithName("GetMetrics")
.Produces<MetricsResponse>(200)
.Produces(500);




// Helpers for mapping & tiny updates
static WorkItemResponse ToResponse(WorkItem e) =>
    new(e.Id, e.Title, e.Service, e.Priority, e.DueDate, e.Status, e.Component, e.Assignee, e.Labels.ToList(), e.CreatedAtUtc, e.UpdatedAtUtc);

app.Run();

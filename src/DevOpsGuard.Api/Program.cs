using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger in Development (default template behavior)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic welcome endpoint (handy to see the app is alive)
app.MapGet("/", () => Results.Ok(new
{
    name = "DevOps Guard API",
    version = "0.1.0",
    message = "Welcome! Swagger is at /swagger, health at /health."
}));

// Simple health endpoint (liveness)
// Later we'll add readiness checks for DB, etc.
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

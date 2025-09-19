
# DevOps Guard
[![CI](https://github.com/DarkishLocket10/devops-guard/actions/workflows/ci.yml/badge.svg)](https://github.com/DarkishLocket10/devops-guard/actions/workflows/ci.yml)



A tiny .NET 8 API that tracks developer work items and exposes simple team KPIs (backlog health, SLA breach rate, risk). Built to demo clean architecture, EF Core, minimal APIs, validation, Swagger, and Docker.

## 🚀 Quickstart (one command)

```bash
docker compose -f docker/docker-compose.yml up -d
# API -> http://localhost:8080/swagger
# DB  -> SQL Server in Docker (port 14333)
````

Click **Authorize** in Swagger and use API key:

```
X-API-Key: dev-secret-123
```

Optional seed:

```
POST /dev/seed
```

## 🧭 API

* Swagger UI: `http://localhost:8080/swagger`
* Core endpoints:

  * `POST /workitems` — create item
  * `GET  /workitems` — list (filters: service, status, assignee; sortBy: updatedAt|priority|dueDate)
  * `GET  /workitems/{id}` — get by id
  * `PATCH /workitems/{id}` — partial update
  * `DELETE /workitems/{id}` — delete
  * `GET  /metrics` — KPIs (backlogHealthPct, slaBreachRatePct, overdueCount, risk.avg)

### Example: create a work item

```bash
curl -X POST http://localhost:8080/workitems \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-secret-123" \
  -d '{
    "title":"Fix NRE in billing webhook",
    "service":"billing",
    "priority":"High",
    "dueDate":"2025-09-30",
    "component":"billing-api",
    "assignee":"alex",
    "labels":["bug","payments"]
  }'
```

## 🛠️ Local dev (without Docker)

Requirements: .NET 8 SDK, SQL Server (or change connection string)

```bash
# bring up just SQL (optional: use Docker)
docker compose -f docker/docker-compose.yml up -d sqlserver

# run API locally
dotnet run --project src/DevOpsGuard.Api
# API -> http://localhost:5191/swagger
```

Set API key in `src/DevOpsGuard.Api/appsettings.Development.json`:

```json
{ "ApiKey": "dev-secret-123" }
```

## 🧩 Tech

* .NET 8 minimal APIs + Swagger (OpenAPI)
* EF Core (SQL Server) with migrations
* Validation: FluentValidation + ProblemDetails
* Auth: simple `X-API-Key` filter
* Docker: multi-stage build + compose

## 🗂️ Structure

```
src/
  DevOpsGuard.Api/            # minimal API + filters + Swagger
  DevOpsGuard.Application/    # DTOs, abstractions, validation
  DevOpsGuard.Domain/         # entities, enums, domain logic
  DevOpsGuard.Infrastructure/ # EF Core DbContext, repositories, configs
docker/
  docker-compose.yml          # sqlserver + api
```

## ✅ Roadmap (next)

* `/events/ingest` to update risk & metrics from CI/CD signals
* Postgres read model (optional) for reporting
* GitHub Actions CI (build, test, docker)
* Tests: unit + integration (WebApplicationFactory)
  "@ | Out-File -Encoding UTF8 README.md

````

## 17.2 Commit
```bash
git add README.md
git commit -m "docs: add recruiter-ready README with quickstart and API tour"
````

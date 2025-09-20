
# DevOps Guard
[![CI](https://github.com/DarkishLocket10/devops-guard/actions/workflows/ci.yml/badge.svg)](https://github.com/DarkishLocket10/devops-guard/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-framework-512BD4) ![C#](https://img.shields.io/badge/C%23-language-9B4F96)


A compact .NET 8/C# system that tracks software engineering work items, exposes risk & SLA KPIs, and ships with a tiny dashboard. It’s designed to show end-to-end skills: API design, EF Core, SQL, validation, background jobs, Docker, CI, and a minimal UI.

---

## 🚀Features

* **Minimal API (.NET 8)** with OpenAPI/Swagger and sample payloads
* **Work items CRUD** (+ filters, paging, sorting): service, status, assignee, priority, due date, labels
* **Validation** with FluentValidation + consistent ProblemDetails errors
* **EF Core + SQL Server** (code-first migrations; typed enums; custom `List<string>` value mapping)
* **Metrics** endpoint: backlog health %, SLA breach %, overdue count, simple risk (priority + overdue)
* **Metrics history**: persisted daily snapshots + CSV export
* **Background service** to auto-capture a snapshot once/day at a configured hour
* **Events ingest**: apply rules from CI/ops signals (e.g., `build_failed`, `incident_opened`)
* **API key** protection (simple header filter) for modifying endpoints
* **Static dashboard** (no Node) at `/dashboard` using Chart.js + fetch
* **Docker Compose** for API + SQL; **GHCR** image + **GitHub Actions** CI (build, test placeholder, Docker build)



## 💻Quick Start (Docker)

> You already moved your `.env` next to the compose file (`docker/.env`). Keep it there.

1. **Create `docker/.env`** (do not commit the real one):

```
# SQL
SA_PASSWORD= strong-password
SQL_PORT=14333

# API
API_KEY= dev-secret
USE_SQLSERVER=true

# Metrics scheduler
METRICS_AUTOCAPTURE=true
METRICS_SNAPSHOT_HOUR_LOCAL=9
```

2. **Start**:

```bash
docker compose -f docker/docker-compose.yml up -d
```

3. **Seed demo data** (first run only):
   Open [http://localhost:8080/swagger](http://localhost:8080/swagger) → **Authorize** → `X-API-Key = dev-secret-123` → `POST /dev/seed`.

4. **Dashboard**: [http://localhost:8080/dashboard](http://localhost:8080/dashboard)
   Paste the same API key → **Load** (and try **Capture Snapshot** if you added that optional button).
   You can also download: **/metrics/history.csv?days=14**.

> SQL container must be **healthy**. If you change `SA_PASSWORD`, bring stack down with `-v` to reset the data volume:
> `docker compose -f docker/docker-compose.yml down -v && docker compose -f docker/docker-compose.yml up -d`



## ⚙️Configuration

The API reads configuration from **environment variables** (compose sets these). Development JSON has placeholders only.

* `UseSqlServer` = `true|false`
* `ConnectionStrings__Default` – e.g. `Server=sqlserver,1433;Database=DevOpsGuard;User Id=sa;Password=...;TrustServerCertificate=True;`
* `ApiKey` – string used by the API-key filter (`X-API-Key`)
* `Metrics__AutoCapture` = `true|false` (background daily snapshot)
* `Metrics__SnapshotHourLocal` = integer 0..23 (local hour)



## 🤖API Overview

* **Auth**: pass `X-API-Key: <value>` for protected endpoints (create/update/delete/ingest/seed/snapshot and CSV if you protected it).
  Read-only endpoints like `/health` are public; you may choose to protect `/metrics`/`/metrics/history`.

* **Swagger/OpenAPI**: [http://localhost:8080/swagger](http://localhost:8080/swagger)

### Work Items

```
GET    /workitems
GET    /workitems/{id}
POST   /workitems
PATCH  /workitems/{id}
DELETE /workitems/{id}
```

**Query params (list):**

* `service` (string), `status` (`Open|InProgress|Blocked|Resolved`), `assignee` (string)
* `page` (default 1), `pageSize` (1–100; default 20)
* `sortBy` (`updatedAt|priority|dueDate`; default `updatedAt`)
* `sortDir` (`asc|desc`; default `desc`)

**Create (JSON):**

```json
{
  "title": "Add rate limiting to gateway",
  "service": "api-gateway",
  "priority": "High",
  "dueDate": "2025-10-01",
  "component": "gateway",
  "assignee": "jamie",
  "labels": ["security","p0"]
}
```

**Partial Update (JSON):**

```json
{ "status": "Resolved", "priority": "P0", "labels": ["incident"] }
```

### Metrics

```
GET /metrics
GET /metrics/history?days=14
GET /metrics/history.csv?days=14
```

* `days`: positive int (capped at 90).
* CSV headers: `capturedAtUtc,backlogHealthPct,slaBreachRatePct,overdueCount,riskAvg`.

### Events Ingest (simulates CI/ops)

```
POST /events/ingest
```

**Request:**

```json
{
  "workItemId": "GUID",
  "kind": "build_failed",
  "source": "github-actions",
  "message": "Build failed on main",
  "occurredAtUtc": "2025-09-19T12:00:00Z"
}
```

Supported kinds & rules:

* `build_failed` → add label `build-failed`, set status `InProgress`, raise priority to at least `High`
* `incident_opened` → add `incident`, set status `Blocked`, set priority `P0`
* `deploy_succeeded` → add `deploy-ok`
* `coverage_dropped` → add `qa`, raise priority to at least `Medium`

### Dev Helpers

```
POST /dev/seed                // adds a few demo items
POST /dev/metrics/snapshot    // store a metrics snapshot "now"
GET  /health
```

> These are protected by API key.



## 📈Dashboard

* Served from `wwwroot`: **/dashboard** (static HTML, Chart.js)
* Controls: API key, days window, load, CSV download, filter + sort, list + delete, edit, quick actions (Resolve / P0)
* Calls the same JSON endpoints as Swagger



## 📊Data Model

**WorkItem**

* `Id: Guid`, `Title`, `Service`, `Priority (Low|Medium|High|P0)`, `Status (Open|InProgress|Blocked|Resolved)`
* `DueDate: DateOnly?`, `Component?`, `Assignee?`, `Labels: List<string>` (stored as comma-joined string)
* `CreatedAtUtc`, `UpdatedAtUtc` (updated on change)

**MetricsSnapshot**

* `Id: Guid`, `CapturedAtUtc`, `BacklogHealthPct`, `SlaBreachRatePct`, `OverdueCount`, `RiskAvg`

**Risk** (simple & transparent)

* `base(priority)` + `3 * daysOverdue`, clamped 0..100
  (`Low=10, Medium=25, High=50, P0=70`)



## 🤔Development

Set env vars or use **user-secrets** (recommended) so you don’t commit secrets.

```bash
cd src/DevOpsGuard.Api
dotnet user-secrets init
dotnet user-secrets set "UseSqlServer" "true"
dotnet user-secrets set "ApiKey" "dev-secret-123"
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,14333;Database=DevOpsGuard;User Id=sa;Password=Your_Strong!Passw0rd_123;TrustServerCertificate=True;"

dotnet run
```

Swagger: [http://localhost:5191/swagger](http://localhost:5191/swagger) (or the port shown).
If you see `Login failed for user 'sa'`, ensure your SQL container is healthy and the password matches the container’s initialized password (changing it requires recreating the volume).


## 🦖Migrations

```bash
# list
dotnet ef migrations list -s src/DevOpsGuard.Api -p src/DevOpsGuard.Infrastructure

# add
dotnet ef migrations add AddSomething -s src/DevOpsGuard.Api -p src/DevOpsGuard.Infrastructure -o Data/Migrations

# apply
dotnet ef database update -s src/DevOpsGuard.Api -p src/DevOpsGuard.Infrastructure
```

> If a migration name already exists, choose a different one (EF requires unique names).


## 📦CI & Container Images

* **GitHub Actions** workflow builds, tests, and builds the Docker image.
* Pushing to **GHCR** is enabled and uses a **lowercased** image path:

  ```
  ghcr.io/<owner>/<repo>/devopsguard-api:latest
  ```

  Example for `DarkishLocket10/devops-guard`:

  ```
  docker pull ghcr.io/darkishlocket10/devops-guard/devopsguard-api:latest
  ```



## 🧪Testing

> If you’ve added the optional tests:
> `tests/DevOpsGuard.Api.Tests` uses `WebApplicationFactory<Program>` to boot the API **in-memory** with the **in-memory repository** (`UseSqlServer=false` override).
> Run:

```bash
dotnet test -c Release
```


## 🔐Security Notes

* **Do not commit** real secrets. Use `docker/.env` locally (ignored by git) and keep `appsettings.Development.json` with placeholders.
* If you ever accidentally committed a real secret, **rotate** it and (optionally) rewrite history with git-filter-repo or BFG.
* GitHub **Secret Scanning** & **Push Protection** are recommended for public repos.
* The included API-key filter is intentionally simple for demos; consider OAuth/OIDC for real systems.


## 🛣️Roadmap

* Unit tests for risk math (pure function)
* Small role model (read-only vs. admin) on API key
* SQLite option for easy local dev without Docker
* React/Blazor richer UI (table sorting/filters, charts)
* Export OpenAPI client (e.g., TypeScript) from Swagger



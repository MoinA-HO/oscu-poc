# Referral and Case Management

Referral CRUD for the Home Office, built from the Automation Centre
`ac-dotnet-api-template` and `ac-react-template`.

```
referral-management
├── api/                 .NET 10, Clean Architecture, Carter minimal API
├── ui/                  React 19 + TypeScript + Vite
├── docs/adr/            architecture decision records
└── docker-compose.yml   PostgreSQL 18 + pgAdmin
```

## ⚠️ Before anything else

The .NET side has never been restored or compiled — it was written in an
environment with no access to nuget.org. Every package id and version in
`Directory.Packages.props` has since been checked to exist, but a transitive
conflict or a compile error is still entirely possible. Treat the first build
as a review step.

The UI **has** been installed, type-checked, linted, built and tested here;
57 tests pass.

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker (for PostgreSQL, pgAdmin and the integration tests)

## Getting started

### 1. Database

```bash
docker compose up -d
```

- PostgreSQL on `localhost:5432` (`postgres` / `postgres`, database `referral_dev_db`)
- pgAdmin on <http://localhost:8080> (`admin@localhost.com` / `admin`)

The compose file has a healthcheck, so `docker compose up -d` does not return
until Postgres is actually accepting connections.

### 2. Create the migration

The schema is **code first**: entities and `IEntityTypeConfiguration` classes
are the source of truth, and the migration is generated from them. No
migrations are checked in yet, so generate the first one:

```bash
cd api
dotnet tool install --global dotnet-ef        # once
dotnet ef migrations add InitialCreate \
  --project src/OSCU.Persistence/OSCU.Persistence.csproj \
  --startup-project src/OSCU.Api/OSCU.Api.csproj
dotnet ef database update \
  --project src/OSCU.Persistence/OSCU.Persistence.csproj \
  --startup-project src/OSCU.Api/OSCU.Api.csproj
```

This is deliberately not pre-written: a hand-authored model snapshot that
disagrees with the real model causes permanent "pending model changes" errors,
and the CLI produces a guaranteed-correct one.

### 3. Run the API

```bash
cd api
dotnet run --project src/OSCU.Api/OSCU.Api.csproj
```

- Swagger: <https://localhost:7500/swagger>
- Health: <https://localhost:7500/health>

### 4. Run the UI

```bash
cd ui
npm install
npm run dev          # http://localhost:5173
```

Vite proxies `/api` to `http://localhost:7501`, so the browser sees one origin
and CORS does not apply locally. `VITE_API_BASE_URL` stays empty in development
and is set to the API **origin only** (no path) in deployed environments.

## Tests

```bash
cd api && dotnet test          # NUnit; integration tests need Docker
cd ui  && npm test             # Vitest + React Testing Library
cd ui  && npm run test:coverage
```

Unit tests run everywhere. The API integration tests start a real PostgreSQL 18
container via Testcontainers, apply the migrations and exercise the endpoints
over HTTP — so they also prove the migration applies cleanly to an empty
database.

## API

Versioned by query string: `?api-version=1` (defaults to 1 when omitted).

| Method | Route | Returns |
|---|---|---|
| `GET` | `/api/referrals` | `200` paged list |
| `GET` | `/api/referrals/statuses` | `200` permitted statuses |
| `GET` | `/api/referrals/{id}` | `200` / `404` |
| `POST` | `/api/referrals` | `201` / `400` / `409` |
| `PUT` | `/api/referrals/{id}` | `200` / `400` / `404` |
| `DELETE` | `/api/referrals/{id}` | `204` / `404` |
| `GET` | `/health` | `200` / `503` |

List parameters: `page`, `pageSize` (max 200), `search`, `status`,
`sortBy` (`ReceivedDate` \| `CreatedDate` \| `ReferralReference` \| `Subject` \| `Status`),
`sortDirection` (`Ascending` \| `Descending`).

Success responses use the envelope `{ "isSuccess": true, "data": ... }`.
Failures are RFC 7807 ProblemDetails carrying a machine-readable `code`.

## Architecture

Dependencies point inwards. Nothing in `Domain` references anything.

```
Api  ──►  Application  ──►  Domain
 │            ▲   ▲
 └──►  Persistence  Infrastructure
```

| Project | Holds |
|---|---|
| `OSCU.Domain` | `Referral`, `ReferralStatus`. No packages at all — **no EF attributes in the model**. |
| `OSCU.Application` | `IReferralService`, `IReferralRepository`, DTOs, validators, `Result`/`Error`, `PagedResult` |
| `OSCU.Persistence` | `ApplicationDbContext`, `IEntityTypeConfiguration` mappings, `ReferralRepository` |
| `OSCU.Infrastructure` | `IDateTimeProvider`; the Azure services on the roadmap land here |
| `OSCU.Api` | Carter modules, validation filter, exception handler, Result→HTTP mapping |

Adding the next table means adding an entity, a configuration, a repository, a
service and a Carter module — five files in five known places, and nothing
central to edit.

### Decisions

- [ADR 0001](docs/adr/0001-carter-minimal-api-over-mvc-controllers.md) — Carter instead of the template's MVC controllers
- [ADR 0002](docs/adr/0002-service-and-repository-over-cqrs.md) — service + repository, not CQRS, and how to change our minds cheaply
- [ADR 0003](docs/adr/0003-react-state-management.md) — server / URL / form / ephemeral state
- [ADR 0004](docs/adr/0004-design-system-adapter-layer.md) — design system behind an adapter, and the DataTable question

## Notable choices

- **No EF attributes on entities.** All mapping is fluent, in
  `Persistence/Configurations/`. `OSCU.Domain.csproj` has zero
  package references, so EF is not even resolvable from the domain.
- **Version 7 GUIDs** for primary keys. Random v4 keys fragment the Postgres
  B-tree as the table grows; v7 is time-ordered.
- **`timestamp with time zone`** columns, and `DateTime` values are required to
  be UTC at the domain boundary. Npgsql rejects any other `DateTimeKind` at
  save time, and a clear error at the boundary beats an opaque provider
  exception on `SaveChanges`.
- **Total ordering.** Every sort has `ThenBy(Id)`, so a row cannot appear on
  both page 1 and page 2.
- **Page size capped at 200.** An uncapped page size is a denial-of-service
  vector.
- **Central Package Management.** One version per package, solution-wide.
- **The clock is injected** via `IDateTimeProvider`, implemented by
  `SystemDateTimeProvider` in Infrastructure. `CreatedDate` is stamped by the
  server so a client cannot backdate a referral, and pinning the clock lets the
  test assert an exact instant instead of a tolerance window that would flake on
  a slow CI agent. The implementation returns `UtcNow` and never `Now` — `Now`
  carries `Kind = Local`, which the domain guard rejects and Npgsql refuses to
  write to a `timestamp with time zone` column.

## Known assumptions to confirm

- **Referral reference format** is assumed to be `REF-` plus at least four
  digits, validated by regex in `CreateReferralRequestValidator` and mirrored
  in the UI. The spec does not state a format. One constant and one regex to
  change.
- **Status vocabulary** is New / In Progress / On Hold / Closed / Rejected,
  stored as text with a validated set (`ReferralStatus`).
- **Description is optional**; empty and whitespace-only values normalise to
  `NULL`.
- **No authentication.** The templates wire up Entra ID; the spec does not ask
  for it, so it is left out rather than half-configured. Add
  `Microsoft.Identity.Web` to the API and MSAL to the UI when it is needed.

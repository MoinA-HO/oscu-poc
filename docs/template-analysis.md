# Template analysis

Findings from reviewing `ac-dotnet-api-template` and `ac-react-template` before
scaffolding this project. Recorded so the decisions in `docs/adr/` have their
evidence attached, and because several items are worth feeding back to the
template maintainers.

## ac-dotnet-api-template

.NET 10, Clean Architecture, six source projects plus six test projects.

### Adopted

- Layered project structure with inward dependency flow
- `Add{Layer}Dependencies()` composition in `Program.cs`
- `Result` / `Error` for expected failures
- Serilog configured from `appsettings`
- Global exception handler returning RFC 7807 ProblemDetails
- Query-string API versioning and per-version Swagger documents
- `docker-compose` with PostgreSQL 18 and pgAdmin
- NUnit + Moq, per-layer test projects, coverlet → opencover for Sonar

### Defects found

| # | Issue | Detail | Handled here by |
|---|---|---|---|
| 1 | Duplicate `PackageReference` | `Template.csproj` references `Microsoft.AspNetCore.OpenApi` at both `10.0.4` and `10.0.6` | Central Package Management |
| 2 | Namespace ≠ assembly | `Result.cs` / `Error.cs` sit in `Template.Application` but declare `namespace Template.Domain.Core` | Moved to `Application.Common.Core` |
| 3 | Broken docs | `readme.md` and `docs/postgres-setup.md` both reference `docker-compose.postgres.yml`; the file is `docker-compose.development.yml` | README matches reality |
| 4 | Dead test | `TemplateDbContextTests.cs` is entirely commented out and references `TemplateDbContext`, which does not exist — the class is `AppDbContext` | Real repository tests |
| 5 | DI landmine | `AddInfrastructureDependencies()` unconditionally registers `GraphServiceClient` resolving `ITokenAcquisition`, which throws when `AzureAD:Enabled` is `false` | Graph removed; nothing registered unconditionally that cannot resolve |
| 6 | Config drift | `appsettings.Development.json` sets `AzureAd:Enabled: true`, so a fresh clone demands an Entra tenant to run locally | No auth configured at all |
| 7 | Silent Serilog config | `appsettings` enriches with `WithMachineName` and `WithThreadId`, but the enricher packages are never referenced, so Serilog ignores them | Enricher packages referenced |
| 8 | Version drift | No central package management; EF Core `10.0.6` and Npgsql `10.0.1` pinned independently across four projects | `Directory.Packages.props` |
| 9 | Duplicated test config | NUnit, the adapter, test SDK and coverlet copy-pasted into six `.csproj` files (~15 lines each) | `tests/Directory.Build.props` |
| 10 | Information disclosure | The exception handler puts `exception.Message` into the response `Detail`, exposing connection strings, paths and internal type names to the caller | Message logged; caller gets a correlation reference only |
| 11 | Demo code in the host | `DbTestController` is marked "DELETE THIS FROM A REAL PROJECT" and exists to prove Azure DB connectivity | Replaced with a real `/health` check |
| 12 | No healthcheck in compose | `docker compose up -d` returns before Postgres accepts connections, so a cold `dotnet ef database update` fails intermittently | `pg_isready` healthcheck |

### The DataTable plumbing

`DataTableResultExtension.cs` (16KB) plus `DataTableQueryParamsModelBinder.cs`
implement a string-encoded query protocol —
`sort=Prop:asc&filter=Prop:op:value` — parsed by a custom model binder and
executed through `System.Linq.Dynamic.Core` and LinqKit.

Not adopted. Evaluating a caller-supplied string as an expression against the
data model is an injection surface, and it removes compile-time checking of
column names. This project uses a typed `ReferralListQuery` with an enum
`sortBy`. See ADR 0004 for how that still leaves the HODS `DataTable`
component usable.

### Removed as example code

`ExampleController`, `ExampleV2Controller`, `AuthController`, `DbTestController`,
`PeoplePickerController`, the entire `Template.MockData.DeleteInProd` project,
and `GraphService`.

## ac-react-template

Vite 7, React 19, TypeScript 5.9 strict, SCSS, MSAL.

### The two unused dependencies

`@tanstack/react-query` and `react-hook-form` are both in `package.json` and
**neither is imported anywhere**. There is no `QueryClientProvider`. Every page
hand-rolls `useState` + `useEffect` + `fetch`.

They answer the state management question the spec asks — they were simply
never plugged in. See ADR 0003.

### Defects found

| # | Issue | Detail | Handled here by |
|---|---|---|---|
| 1 | **No test infrastructure at all** | No Vitest, no RTL, no jsdom, no test script. Fatal for test-first work on the front end | Vitest + RTL + jsdom; 57 tests |
| 2 | Doubled API path | `.env` sets `VITE_API_BASE_URL=https://localhost:7500/api/data-table`, then `DataTableApi.ts` appends `/api/data-table` again | Base URL is origin-only; documented in `.env.example` |
| 3 | `lint` script, no config | `package.json` runs `eslint .` with no `eslint.config.js` present, so it fails on a clean clone | `eslint.config.js` added |
| 4 | Forced login on boot | `main.tsx` calls `loginRedirect` before rendering, so the app cannot start without an Entra tenant | No auth wired up |
| 5 | `any` leakage | `PeoplePicker.tsx` maps `(u: any)`; the MSAL logger callback types every parameter as `any` | `@typescript-eslint/no-explicit-any` set to error |
| 6 | Structure by type | `pages/` `services/` `types/` does not scale to 20 tables | Feature folders |
| 7 | Unsafe array indexing | `noUncheckedIndexedAccess` off, so every out-of-bounds read is invisible to the compiler | Enabled |

### Private feed

`@ukhoautomation/hods-react` resolves from a private Azure DevOps feed that is
not currently reachable. Rather than block, the design system sits behind an
adapter layer backed by the public `govuk-frontend`. See ADR 0004.

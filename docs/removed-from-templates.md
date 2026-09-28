# What I removed from the templates

A file-by-file account of everything dropped or rewritten when scaffolding this
project from `ac-dotnet-api-template` and `ac-react-template`.

I removed things using the test "does this look like demo scaffolding?" when
the test should have been "is this an Automation Centre standard?". Three
items have already been identified as wrong calls (DataTable, auth, Graph).
This is the rest of the list so the remainder can be checked in one pass.

**Status key**

| | |
|---|---|
| ❌ | Removed entirely |
| ♻️ | Replaced — same purpose, different implementation |
| ✏️ | Kept, with changes |
| ✅ | Kept as-is |

The **Standard?** column is for you.

---

## 1. Build, deploy and quality gates

**This is the section I would look at first.** None of it was carried over, and
none of it was flagged at the time. Without it the project cannot deploy the AC
way or report to SonarQube.

| Status | File | What it actually is | Standard? |
|---|---|---|---|
| ❌ | `api/.azdo/deploy.yml` | Azure DevOps pipeline. Publishes `src/Template`, deploys to a staging slot then swaps to live — described in the file itself as "following existing AC practice". Parameterised by environment; targets `rg-sasa-templatelibrary-*` / `app-templatelibrary-api-*` via the `cto-ho1-automation-*` service connection. | |
| ❌ | `api/.azdo/sonar.yml` | SonarQube analysis pipeline. Triggers on all branches **and all PRs**. Uses the `SonarQube-ACTemplates` service connection, runs `dotnet test` with coverage and publishes results. | |
| ❌ | `api/.azdo/coverage.runsettings` | Coverlet config emitting **opencover** format, which is the format `sonar.yml` consumes. Useless without the pipeline, and the pipeline is useless without it. | |
| ❌ | `ui/.azdo/deploy.yml` | React pipeline: Node 25, `npm ci`, `npm run build`, deploy to `app-templatelibrary-ui-*`. Note it injects `VITE_AAD_CLIENT_ID` as a build-time env var — **the client ID GUID is hardcoded in the YAML**, which is worth a look regardless of what you decide here. | |
| ❌ | `api/.vscode/launch.json` | Debug launch config for the API project. | |

All four YAML files have template-specific resource names baked in
(`templatelibrary`, `AC-dotnet-template`) so they need renaming for OSCU, but
that is a find-and-replace, not a rewrite.

---

## 2. Shared contracts

| Status | File | What it actually is | Standard? |
|---|---|---|---|
| ❌ | `Application/Extensions/DataTable/DataTableQueryParams.cs` | The query DTO: `Page`, `PageSize`, `Sort`, `Filters`. | ✔ confirmed |
| ❌ | `Application/Extensions/DataTable/DataTableResultSet.cs` | Response shape: `Rows`, `Total`, `RowIdProperty`. | ✔ confirmed |
| ❌ | `Application/Extensions/DataTable/DataTableResultExtension.cs` | 16KB. `ToDataTableResult(query)` over `IQueryable`, applying filter/sort/page via `System.Linq.Dynamic.Core` + LinqKit. Supports 11 filter operators across `string`, `DateTime`, `bool`, `int`. | ✔ confirmed |
| ❌ | `Api/ModelBinders/DataTableQueryParamsModelBinder.cs` + `Provider.cs` | Parses the query string into `DataTableQueryParams`, registered in `Program.cs`. **Cannot be restored as-is** — it is an MVC `IModelBinder` and Carter minimal APIs do not run those. Needs a static `BindAsync` instead. | ✔ confirmed |
| ❌ | `Api/Controllers/V1/DataTableController.cs` | The endpoint wiring the above together. | ✔ confirmed |
| ❌ | **`docs/data-table-results.md`** | **The contract specification.** 5KB documenting every query param, all 11 operators and their aliases, and — importantly — the `rowIdProperty` row-selection-persistence rule that front ends must follow so selection survives sort/filter/page. This is the document a consumer reads. Dropping it was worse than dropping the code. | |
| ❌ | `tests/.../DataTableResultsExtensionUnitTests.cs` | 31KB of real tests over the extension. The single largest body of test coverage in the template. | |
| ❌ | `tests/.../DataTableQueryParamsModelBinderUnitTests.cs` | 13KB of binder parsing tests. | |
| ❌ | `ui/src/types/DataTableTypes.ts` | Client types mirroring the contract. | ✔ confirmed |
| ❌ | `ui/src/services/api/DataTableApi.ts` | Client serialiser producing the `sort=`/`filter=` strings. | ✔ confirmed |
| ❌ | `ui/src/pages/data-table/DataTableExample.tsx` | Reference implementation against the HODS `DataTable` component. | ✔ confirmed |

One inconsistency to be aware of when restoring: `docs/data-table-results.md`
shows the envelope as `{"success": true, ...}`, but `Result<T>` actually
serialises `isSuccess`, and `DataTableTypes.ts` expects `isSuccess`. The doc is
out of date, not the code.

---

## 3. Identity and Graph

| Status | File | What it actually is | Standard? |
|---|---|---|---|
| ❌ | `Infrastructure/Services/GraphService.cs` + `IGraphService.cs` | Wraps `GraphServiceClient`, built from `ClientSecretCredential` using `AzureAd:TenantId/ClientId/ClientSecret` — **app-only** auth. | ✔ confirmed |
| ❌ | `Api/Controllers/PeoplePickerController.cs` | `GET /api/people?query=` — Entra user search by `displayName`, `givenName`, `userPrincipalName`, `surname`. | ✔ confirmed |
| ❌ | Azure AD block in `Program.cs` | `Microsoft.Identity.Web` JWT bearer, gated behind `AzureAD:Enabled`. | ✔ confirmed |
| ❌ | `Api/Controllers/AuthController.cs` | `[Authorize]` smoke-test endpoint proving the token flow works end to end. | ✔ confirmed |
| ❌ | `ui/src/auth/*` (3 files) | MSAL config, `PublicClientApplication`, `MsalProvider` wrapper. | ✔ confirmed |
| ✏️ | `ui/src/services/apiClient.ts` | Kept, but the `acquireTokenSilent` bearer injection was stripped out. | ✔ confirmed |
| ❌ | `ui/src/services/authApi.ts` | Calls `/Auth` — the client half of the smoke test. | ✔ confirmed |
| ❌ | `ui/src/services/graphToken.ts` | Direct Graph calls with `User.ReadBasic.All`, silent-then-popup token acquisition. | ✔ confirmed |
| ❌ | `ui/src/pages/people-picker/PeoplePicker.tsx` + `.scss` | Reference implementation against the HODS `PeoplePicker` component. | ✔ confirmed |

**Worth knowing before restoring:** the template registers Graph **twice**, two
different ways. `Infrastructure/DependencyInjection.cs` registers a
`GraphServiceClient` using `ITokenAcquisition` (**delegated** — acts as the
signed-in user), while `GraphService` builds its own using
`ClientSecretCredential` (**app-only** — acts as the application).
`PeopleController` injects `IGraphService`, so the app-only one wins and the
delegated registration is dead. Restoring both as-is carries that over. Which
one you actually want is a real decision, and it affects what Entra permissions
the app needs.

---

## 4. Demo and sample code

I am reasonably confident about this section, but listing it so you can
disagree.

| Status | File | What it actually is | Standard? |
|---|---|---|---|
| ❌ | `src/Template.MockData.DeleteInProd/` (whole project, 10 files) | Fake `People` data, its own `DbContext` and 3 migrations. Named `DeleteInProd`; the `Program.cs` call site is commented `// TODO: DELETE THIS FROM A REAL PROJECT!!!`. Only consumer was `DataTableController`, so the DataTable restore needs a real data source instead. | |
| ❌ | `Api/Controllers/DbTestController.cs` | `GET /api/dbtest/ping`. Comment reads `// TODO : DELETE THIS CONTROLLER FROM A REAL PROJECT!!!`. Replaced here by a real `/health` check with `AddDbContextCheck`. Note it never actually queried the database — it returned a success string unconditionally. | |
| ❌ | `Api/Controllers/V1/ExampleController.cs` | Returns `[1,2,3]`, plus a `/Error` endpoint that throws to demo the handler. | |
| ❌ | `Api/Controllers/V2/ExampleV2Controller.cs` | Empty `Ok()` demonstrating v2 routing. | |
| ❌ | `tests/.../ExampleControllerTests.cs` | Tests the above. | |
| ❌ | `tests/.../TemplateDbContextTests.cs` | 100% commented out, and references `TemplateDbContext` which does not exist (the class is `AppDbContext`). | |
| ❌ | `ui/src/pages/home/HomePage.tsx` + `.scss` | "Hello, {username}. You have successfully logged in 👋" plus a Call API button. Auth smoke test — if auth returns, some equivalent landing page is needed. | |
| ❌ | `ui/public/vite.svg`, `ui/src/assets/react.svg` | Vite scaffold boilerplate. | |
| ❌ | `Domain/DependencyInjection.cs` | `AddDomainDependencies()` — registered nothing, existed only to be called. | |
| ❌ | `tests/Unit/Template.Infrastructure.UnitTests/` | Empty project, no test files. | |

---

## 5. Docs and conventions

| Status | File | What it actually is | Standard? |
|---|---|---|---|
| ❌ | **`.github/copilot-instructions.md`** | 6.5KB of agent conventions: layer rules, `Result` over exceptions, NUnit + Moq, naming, logging to ELK, Key Vault for secrets, PR checklist. The template's own readme says **"Update the copilot-instructions.md file to reflect your project's specifics"** — it is meant to be edited, not deleted. Deleting it was a straight misread. | |
| ❌ | **`docs/adr/adr-template.md`** | The AC ADR format. I wrote four ADRs in my own layout instead of theirs, so they will not match your other repos. | |
| ❌ | `docs/postgres-setup.md` | Folded into the README rather than kept as a separate file. | |
| ♻️ | `readme.md` | Rewritten for this project. | |
| ♻️ | `docker-compose.development.yml` | Renamed `docker-compose.yml`, added a `pg_isready` healthcheck, renamed the database. | |

---

## 6. Kept, with changes

For completeness — nothing here was dropped.

| Status | File | Change |
|---|---|---|
| ✏️ | `Application/Core/Result.cs`, `Error.cs` | Namespace corrected to match the assembly; `ErrorType` enum added so the transport layer can map failures to status codes. |
| ✏️ | `Api/Middleware/ExceptionHandlingMiddleware.cs` | No longer puts `exception.Message` in the response body; logs it and returns a correlation reference instead. |
| ✏️ | `Api/Config/SwaggerConfigureOptions.cs` | Same per-version document generation, plus title and description. |
| ✏️ | `ui/tsconfig.app.json` | Added `noUncheckedIndexedAccess`. |
| ✅ | `ui/.npmrc`, `ui/.prettierrc`, `api/Properties/launchSettings.json` | Unchanged. |

---

## Packages dropped

**API:** `Microsoft.Graph` 4.53.0, `Microsoft.Identity.Web` 4.7.0,
`Azure.Identity` 1.20.0, `LinqKit` 1.3.11, `System.Linq.Dynamic.Core` 1.7.2

**UI:** `@azure/msal-browser`, `@azure/msal-react`,
`@ukhoautomation/hods-react`

---

## Summary

- **Already confirmed as wrong calls:** DataTable contract, auth, Graph.
- **Most likely also wrong, not yet raised:** the four `.azdo` pipeline files,
  `copilot-instructions.md`, `docs/data-table-results.md`, `docs/adr/adr-template.md`.
- **Probably genuinely right:** the MockData project, the Example and DbTest
  controllers, the empty `Domain/DependencyInjection.cs`, the empty
  Infrastructure test project, and the Vite boilerplate SVGs.

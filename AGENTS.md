# AGENTS.md

## Tech Stack

- .NET 10, C# 14
- .NET Aspire 13.5 (orchestration, not just service discovery)
- FastEndpoints (NOT Minimal APIs / NOT Controllers)
- EF Core + PostgreSQL (via Aspire Npgsql)
- Amazon S3 (Ministack for local dev)
- TUnit for tests (NOT xUnit/NUnit)
- Vogen for value objects
- ErrorOr for error handling
- Roslynator analyzers (build-enforced)

## Solution Structure

| Project | Role |
|---|---|
| `src/Bulkivore.Api` | Main API. FastEndpoints, EF Core, Domain logic |
| `src/Bulkivore.AppHost` | Aspire orchestrator. Defines resources, wiring, Docker Compose |
| `src/Bulkivore.MigrationService` | Worker that runs EF Core migrations at startup |
| `src/Bulkivore.ServiceDefaults` | Shared Aspire defaults (OpenTelemetry, resilience, service discovery) |
| `test/Bulkivore.IntegrationTests` | Integration tests using TUnit + AspireFixture |

## Build & Test

```bash
dotnet build Bulkivore.slnx        # Build entire solution
dotnet test test/Bulkivore.IntegrationTests  # Run integration tests
```

- **`TreatWarningsAsErrors` is ON** globally via `Directory.Build.props`. Any warning = build failure.
- Roslynator analyzers (CodeAnalysis + Formatting) are enforced in build.

## Key Conventions

- **FastEndpoints**: Endpoints live in `src/Bulkivore.Api/Endpoints/`. No `[ApiController]`, no `MapGet`/`MapPost`. Use `Ep.Req<T>.Res<T>` base class.
- **ErrorOr pattern**: Endpoint response DTOs implement `IErrorOr`. The `ResponseSender` post-processor handles this automatically — do NOT manually return error responses from `HandleAsync`.
- **Vogen value objects**: Domain primitives are Vogen types. They require explicit casting (`ValueOf`/`From`). The assembly-level `VogenDefaults` is in `GlobalUsings.cs`.
- **Domain layer**: `src/Bulkivore.Api/Domain/` — pure domain types, no infrastructure references.
- **Infrastructure layer**: `src/Bulkivore.Api/Infrastructure/` — EF Core, AWS, services. DI wiring in `DependencyInjection.cs`.
- **Extension methods for DI**: `extension(IServiceCollection services)` syntax (C# 14).
- **Route prefix**: All API endpoints use `/api` prefix (configured in `Program.cs`).
- **Test DB**: Separate `bulkivore-test-db` database. Connection string injected via Aspire as `TEST_DB_CONN`.

## Testing

- **Framework**: TUnit (NOT xUnit). Use `[Test]`, `[ClassDataSource<T>]`, `await Assert.That(x).Should()`.
- **Aspire integration tests**: `AppFixture : AspireFixture<Projects.Bulkivore_AppHost>` starts the full Aspire stack.
- **Schema isolation**: Tests use `TestSchemaScope` to create/drop unique PostgreSQL schemas per test.
- **FastEndpoints typed HTTP calls**: Use `FastEndpointsAspireExtensions` helpers (`client.PostAsync<TEndpoint, TReq, TRes>(req)`).
- **Test session sharing**: `Shared = SharedType.PerTestSession` for expensive Aspire fixtures.

## Migrations

Use `task` (Taskfile) for EF Core migrations:

```bash
task migration:add -- <MigrationName>   # Create new migration
task migration:remove                    # Rollback last migration
task migration:update                    # Apply pending migrations
task migration:list                      # List all migrations
```

Migrations output to `Infrastructure/Persistence/Migrations`. The `MigrationService` worker applies them at startup via Aspire.

## Aspire Orchestration

The `AppHost.cs` defines:
1. **S3 storage** (Ministack) with bucket initialization
2. **PostgreSQL** with two databases: `bulkivore-db` (main) and `bulkivore-test-db` (tests)
3. **Migration runner** (project in dev, Dockerfile in publish)
4. **API** wired to DB, test DB, S3, with health checks

Local dev: `dotnet run --project src/Bulkivore.AppHost` starts the full stack.

## Skills

- `.opencode/skills/csharp-tunit/SKILL.md` — TUnit best practices: data-driven tests (`[Arguments]`, `[MethodData]`), assertions (`await Assert.That().Should()`), lifecycle hooks (`[Before(Test)]`), parallel execution, migration from xUnit.

# Rules: tests

Read [../RULES.md](../RULES.md) first.

## What to test where

| Project | Tests | Dependencies |
|---|---|---|
| `TemplateApp.Domain.UnitTests` | Every invariant and state transition of every entity, success and failure; `Result<T>` semantics | none |
| `TemplateApp.Application.UnitTests` | Per use case: handler outcomes (success, not found, conflict, propagated domain errors) and validator rules. Pipeline behaviours through the real MediatR registration | EF Core InMemory, `RecordingCache` |
| `TemplateApp.Infrastructure.IntegrationTests` | Behaviour that depends on SQL Server or Redis: migrations, constraints, interceptors, SQL translation of queries, caching | Testcontainers (SQL Server, Redis) |
| `TemplateApp.Api.IntegrationTests` | The HTTP contract: routes, status codes, problem bodies, headers, authentication, complete flows | Testcontainers (SQL Server) |
| `TemplateApp.ArchitectureTests` | Layer dependencies and folder/naming conventions | none |
| `TemplateApp.Tests.Common` | Not a test project. Factories that create valid entities and requests | Domain, Contracts |

A new use case needs at least:
- handler tests and validator tests in Application.UnitTests;
- one success and one failure test per endpoint in Api.IntegrationTests;
- domain tests for any new domain rule;
- an Infrastructure test for any new constraint or index behaviour.

## Placement

Mirror the production folders:
- `Application.UnitTests/Features/<Feature>/{Commands|Queries}/<UseCase>/<Type>Tests.cs`
- `Domain.UnitTests/<Feature>/`
- `Api.IntegrationTests/Controllers/<Feature>ControllerTests.cs`
- `Infrastructure.IntegrationTests/{Data|Caching}/`

Shared fixtures live in each project's `Common/` folder.

## Conventions

- **Names:** `Method_Condition_ExpectedOutcome` (`Handle_UnknownId_ReturnsNotFound`).
- **Shape:** arrange, act, assert, separated by blank lines. One behaviour per test.
- **Test data:** build it with `Tests.Common` factories (`TodoItemFactory`) and override only the values the test is about. Use unique titles or ids, because integration tests share one database per collection.
- **Assertions:** assert on outcomes (returned `Error`, HTTP status, persisted state), not on implementation details. Do not write tests that only restate the code.
- **Isolation:** no production services or shared databases. Containers start per test collection, and Docker is the only requirement.
- **Time:** never depends on the wall clock. Use `FixedTimeProvider` or `TestClock`.
- **No sleeps,** except where the behaviour under test is time-based (cache expiry), and keep those under two seconds.

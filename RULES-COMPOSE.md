# Rules index

This file links every rule file in the repository. **Humans and AI agents must read the applicable rules before editing code.**

## Mandatory reading for AI agents

Before changing any file:

1. Read [RULES.md](RULES.md) (whole solution).
2. Find the files you will touch in the table below and read every rule file listed for them.
3. Follow the workflow in [RULES.md § 2](RULES.md#2-development-workflow-one-use-case-at-a-time) for new behaviour. Do not restructure folders, replace the persistence approach (EF Core through `IAppDbContext`), or introduce new abstraction frameworks unless the user asks for it.
4. After the change, run the checks in [Verification](#verification) and report anything you could not run.

If a request conflicts with a rule, say so and ask before breaking the rule.

## Which rules apply

| You are changing | Read |
|---|---|
| Anything | [RULES.md](RULES.md) |
| `src/TemplateApp.Domain/**` | [src/TemplateApp.Domain/RULES.md](src/TemplateApp.Domain/RULES.md) |
| `src/TemplateApp.Application/**` | [src/TemplateApp.Application/RULES.md](src/TemplateApp.Application/RULES.md) |
| `src/TemplateApp.Contracts/**` | [src/TemplateApp.Contracts/RULES.md](src/TemplateApp.Contracts/RULES.md) |
| `src/TemplateApp.Infrastructure/**` | [src/TemplateApp.Infrastructure/RULES.md](src/TemplateApp.Infrastructure/RULES.md) |
| `src/TemplateApp.Api/**` | [src/TemplateApp.Api/RULES.md](src/TemplateApp.Api/RULES.md) |
| `tests/**` | [tests/RULES.md](tests/RULES.md) |
| A new feature or use case | All of the above, in layer order: Domain → Application → Contracts → Infrastructure → Api → tests |
| `Dockerfile`, `docker-compose.yml`, `deploy/**`, `.github/**` | [RULES.md](RULES.md) §6 and the README sections "Running with Docker" and "CI/CD" |

## Enforcement

Rules that a machine can check are checked on every build and in CI. The others are enforced in review.

| Rule | Enforced by |
|---|---|
| Layer dependencies (Domain, Contracts depend on nothing; Application not on Infrastructure/Api/Contracts; Infrastructure not on Api) | `tests/TemplateApp.ArchitectureTests/LayerDependencyTests.cs` |
| Controllers never use `IAppDbContext`, EF Core or Infrastructure | `LayerDependencyTests.Controllers_GoThroughUseCasesInsteadOfPersistence` |
| `Features/<Feature>/{Commands\|Queries}/<UseCase>/`, one sealed `<Request>Handler`, sealed `<Request>Validator` next to it | `tests/TemplateApp.ArchitectureTests/ConventionTests.cs` |
| DTOs in `Dtos/`, mappers in `Mappers/`; `ICachedQuery` only on queries, `IInvalidatesCache` only on commands | `ConventionTests` |
| Controllers sealed, derived from `ApiController`, in `Controllers/` | `ConventionTests.Controllers_AreSealedAndDeriveFromApiController` |
| No public setters on domain types | `ConventionTests.DomainEntities_DoNotExposePublicSetters` |
| No `DateTime.Now/UtcNow`, `.Result`, `.Wait()`, `Console.WriteLine` in `src/` | `Microsoft.CodeAnalysis.BannedApiAnalyzers` with `src/BannedSymbols.txt` |
| Style, naming, file layout | `.editorconfig` + StyleCop, `TreatWarningsAsErrors` (`Directory.Build.props`) |
| Every model change has a migration | `Infrastructure.IntegrationTests/Data/PersistenceTests.Migrations_MatchTheCurrentModel` |
| Behaviour and HTTP contract | Unit and integration tests, run by `.github/workflows/ci.yml` (architecture tests run first) |

## Verification

```bash
dotnet build                                         # zero warnings (warnings are errors)
dotnet test tests/TemplateApp.ArchitectureTests      # fast boundary check
dotnet test                                          # full suite; Docker must be running
```

## Rule files

- [RULES.md](RULES.md): architecture, workflow, clean code, coupling, performance, security, definition of done
- [src/TemplateApp.Domain/RULES.md](src/TemplateApp.Domain/RULES.md)
- [src/TemplateApp.Application/RULES.md](src/TemplateApp.Application/RULES.md)
- [src/TemplateApp.Contracts/RULES.md](src/TemplateApp.Contracts/RULES.md)
- [src/TemplateApp.Infrastructure/RULES.md](src/TemplateApp.Infrastructure/RULES.md)
- [src/TemplateApp.Api/RULES.md](src/TemplateApp.Api/RULES.md)
- [tests/RULES.md](tests/RULES.md)

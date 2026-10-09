# CLAUDE.md

Before editing anything in this repository, read [RULES-COMPOSE.md](RULES-COMPOSE.md) and every rule file it lists for the paths you will change.

- Follow the existing structure: Clean Architecture, `Features/<Feature>/{Commands|Queries}/<UseCase>/`, controllers deriving from `ApiController`.
- Do not change the persistence approach or add abstraction layers unless asked.
- Verify with `dotnet build` and `dotnet test` (Docker required for integration tests), and report any check you could not run.

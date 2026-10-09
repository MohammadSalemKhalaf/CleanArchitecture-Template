# AGENTS.md

Coding agents: read [RULES-COMPOSE.md](RULES-COMPOSE.md) before making changes, then read every rule file it lists for the paths you will touch.

- Keep the existing layering and feature/use-case folders; architecture tests enforce them.
- New behaviour follows the workflow in [RULES.md § 2](RULES.md#2-development-workflow-one-use-case-at-a-time).
- Done means `dotnet build` passes with zero warnings and `dotnet test` passes. Say which checks you could not run.

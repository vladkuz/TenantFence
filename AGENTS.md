# Repository Guidelines

This repository is a minimal .NET solution used to validate EF Core multi-tenancy invariants. Keep additions small and focused on the correctness harness.

## Project Structure & Module Organization

```
/  (repo root)
  TenantFence.EFCore/          multi-tenant infrastructure types
  TenantFence.EFCore.Tests/    xUnit tests using EF Core SQLite
  README.md
  TenantFence.sln
```

Keep core logic inside `TenantFence.EFCore/` and tests in `TenantFence.EFCore.Tests/`. Avoid adding extra layers or frameworks.

## Build, Test, and Development Commands

- `dotnet build` — builds the full solution.
- `dotnet test` — runs all xUnit tests.

## Coding Style & Naming Conventions

- Indentation: 4 spaces for C#.
- Naming: `PascalCase` for public types and members, `camelCase` for locals.
- Keep files small and single-purpose; one public type per file where possible.

## Testing Guidelines

- Tests use xUnit and live in `TenantFence.EFCore.Tests/`.
- Name test classes `*Tests.cs` and test methods by behavior (e.g., `Cross_tenant_delete_throws`).
- Prefer SQLite file databases under the temp directory to avoid in-memory lifetime quirks.

## Commit & Pull Request Guidelines

- Use Conventional Commits (`feat:`, `fix:`, `chore:`) unless the project adopts a different standard.
- PRs should include a clear summary and any relevant test notes.

## Security & Configuration Tips

- Do not commit secrets. If configuration is needed, add `.env.example` and document variables in `README.md`.

# TenantFence.EFCore

A minimal .NET 10 package that enforces tenant isolation for EF Core change-tracked writes. It is intentionally small: no web host, no UI, and no authentication. The goal is to prove one invariant: multi-tenant entities cannot be saved without an active tenant context, and cross-tenant modifications are blocked.

## Invariant Being Enforced

- A tenant context is required for any `IMultiTenant` change.
- Added entities inherit the active tenant if `TenantId` is empty.
- Modified and deleted entities must match the original tenant id.

## Build and Test

- `dotnet build` — builds the solution.
- `dotnet test` — runs the xUnit tests using SQLite file databases.

## Usage

Use `CurrentTenant.Use` to scope a tenant, then save through a `TenantDbContext`:

```csharp
var currentTenant = new CurrentTenant();
using (currentTenant.Use("tenant-a"))
{
    using var context = TestSqliteDatabase.CreateContext(dbPath, currentTenant);
    context.Widgets.Add(new Widget { Name = "Widget" });
    await context.SaveChangesAsync();
}
```

## Limitations

- `ExecuteUpdate` and `ExecuteDelete` bypass `SaveChanges` and are not protected by these invariants; bulk ops don't use change tracking.
- Detached `IMultiTenant` updates/deletes (e.g., `Attach`/`Update`) are intentionally rejected; load entities in the current `DbContext`.

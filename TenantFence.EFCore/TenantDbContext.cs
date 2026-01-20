using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TenantFence.EFCore.Exceptions;

namespace TenantFence.EFCore;

public abstract class TenantDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;
    // Tracks entities loaded by this context (or newly added) to prevent detached updates/deletes.
    private readonly ConditionalWeakTable<object, TrackedMarker> _trackedEntities = new();

    protected TenantDbContext(DbContextOptions options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        ChangeTracker.Tracked += HandleTracked;
        ChangeTracker.StateChanged += HandleStateChanged;
    }

    public override int SaveChanges()
    {
        EnforceTenantRules();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenantRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceTenantRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceTenantRules()
    {
        var currentTenantId = _currentTenant.Id;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not IMultiTenant multiTenant)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentTenantId))
            {
                throw new TenantContextMissingException();
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    // Added entities either inherit the current tenant or must already match it.
                    if (string.IsNullOrWhiteSpace(multiTenant.TenantId))
                    {
                        multiTenant.TenantId = currentTenantId;
                    }
                    else if (!string.Equals(multiTenant.TenantId, currentTenantId, StringComparison.Ordinal))
                    {
                        throw new TenantMismatchException(multiTenant.TenantId, currentTenantId);
                    }

                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    // Reject detached updates/deletes that bypass original-value tracking.
                    if (!_trackedEntities.TryGetValue(entry.Entity, out _))
                    {
                        throw new InvalidOperationException(
                            "Detached IMultiTenant entities are not supported. Load the entity in this DbContext before modifying or deleting.");
                    }

                    // Modified/deleted entities must match the original tenant value, not a mutated one.
                    var originalTenantId = entry.Property(nameof(IMultiTenant.TenantId)).OriginalValue as string;
                    if (!string.Equals(originalTenantId, currentTenantId, StringComparison.Ordinal))
                    {
                        throw new TenantMismatchException(originalTenantId, currentTenantId);
                    }

                    break;
            }
        }
    }

    private void HandleTracked(object? sender, EntityTrackedEventArgs e)
    {
        if (e.FromQuery && e.Entry.Entity is IMultiTenant)
        {
            _trackedEntities.GetOrCreateValue(e.Entry.Entity);
        }
    }

    private void HandleStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        // Mark newly added entities so they can transition state within this context safely.
        if (e.OldState == EntityState.Detached && e.NewState == EntityState.Added && e.Entry.Entity is IMultiTenant)
        {
            _trackedEntities.GetOrCreateValue(e.Entry.Entity);
        }
    }

    private sealed class TrackedMarker
    {
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TenantFence.EFCore;
using TenantFence.EFCore.Exceptions;
using Xunit;

namespace TenantFence.EFCore.Tests;

public sealed class TenantDbContextTests
{
    private readonly CurrentTenant _currentTenant = new();

    [Fact]
    public async Task Saving_without_tenant_context_throws()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
            context.Widgets.Add(new Widget { Name = "Widget" });

            await Assert.ThrowsAsync<TenantContextMissingException>(() => context.SaveChangesAsync());
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public void Saving_without_tenant_context_throws_sync()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
            context.Widgets.Add(new Widget { Name = "Widget" });

            Assert.Throws<TenantContextMissingException>(() => context.SaveChanges());
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Saving_with_tenant_context_sets_tenant_id()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using (_currentTenant.Use("tenant-a"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var widget = new Widget { Name = "Widget", TenantId = string.Empty };
                context.Widgets.Add(widget);

                await context.SaveChangesAsync();

                Assert.Equal("tenant-a", widget.TenantId);
            }

            using var verifyContext = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
            var count = await verifyContext.Widgets.CountAsync();
            Assert.Equal(1, count);
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Saving_with_wrong_tenant_id_throws()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using (_currentTenant.Use("tenant-a"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                context.Widgets.Add(new Widget { Name = "Widget", TenantId = "tenant-b" });

                var exception = await Assert.ThrowsAsync<TenantMismatchException>(() => context.SaveChangesAsync());
                Assert.Contains("tenant-b", exception.Message);
                Assert.Contains("tenant-a", exception.Message);
            }
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Cross_tenant_modification_throws()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using (_currentTenant.Use("tenant-a"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                context.Widgets.Add(new Widget { Name = "Original" });
                await context.SaveChangesAsync();
            }

            using (_currentTenant.Use("tenant-b"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var widget = await context.Widgets.SingleAsync();
                widget.Name = "Modified";

                var exception = await Assert.ThrowsAsync<TenantMismatchException>(() => context.SaveChangesAsync());
                Assert.Contains("tenant-a", exception.Message);
                Assert.Contains("tenant-b", exception.Message);
            }
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Tenant_id_mutation_does_not_bypass_original_tenant_check()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using (_currentTenant.Use("tenant-a"))
            {
                using var seedContext = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                seedContext.Widgets.Add(new Widget { Name = "Original" });
                await seedContext.SaveChangesAsync();
            }

            using (_currentTenant.Use("tenant-b"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var widget = await context.Widgets.SingleAsync();
                widget.TenantId = "tenant-b";
                widget.Name = "Mutated";

                var exception = await Assert.ThrowsAsync<TenantMismatchException>(() => context.SaveChangesAsync());
                Assert.Contains("tenant-a", exception.Message);
                Assert.Contains("tenant-b", exception.Message);
            }
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Cross_tenant_delete_throws()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            using (_currentTenant.Use("tenant-a"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                context.Widgets.Add(new Widget { Name = "ToDelete" });
                await context.SaveChangesAsync();
            }

            using (_currentTenant.Use("tenant-b"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var widget = await context.Widgets.SingleAsync();
                context.Widgets.Remove(widget);

                await Assert.ThrowsAsync<TenantMismatchException>(() => context.SaveChangesAsync());
            }
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Update_detached_entity_throws()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            var widgetId = 0;
            using (_currentTenant.Use("tenant-a"))
            {
                using var seedContext = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var widget = new Widget { Name = "Seed" };
                seedContext.Widgets.Add(widget);
                await seedContext.SaveChangesAsync();
                widgetId = widget.Id;
            }

            using (_currentTenant.Use("tenant-a"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var detached = new Widget { Id = widgetId, TenantId = "tenant-a", Name = "Updated" };
                context.Update(detached);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
                Assert.Contains("Detached IMultiTenant entities are not supported", exception.Message);
            }
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }

    [Fact]
    public async Task Attach_delete_detached_entity_throws()
    {
        var dbPath = TestSqliteDatabase.CreateUniquePath();
        try
        {
            var widgetId = 0;
            using (_currentTenant.Use("tenant-a"))
            {
                using var seedContext = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var widget = new Widget { Name = "Seed" };
                seedContext.Widgets.Add(widget);
                await seedContext.SaveChangesAsync();
                widgetId = widget.Id;
            }

            using (_currentTenant.Use("tenant-a"))
            {
                using var context = TestSqliteDatabase.CreateContext(dbPath, _currentTenant);
                var detached = new Widget { Id = widgetId, TenantId = "tenant-a", Name = "Seed" };
                context.Attach(detached);
                context.Widgets.Remove(detached);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
                Assert.Contains("Detached IMultiTenant entities are not supported", exception.Message);
            }
        }
        finally
        {
            TestSqliteDatabase.DeleteDatabaseFile(dbPath);
        }
    }
}

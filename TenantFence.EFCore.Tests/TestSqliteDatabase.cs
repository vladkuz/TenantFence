using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TenantFence.EFCore;

namespace TenantFence.EFCore.Tests;

internal static class TestSqliteDatabase
{
    public static string CreateUniquePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "TenantFenceTests");
        Directory.CreateDirectory(root);
        return Path.Combine(root, $"test-{Guid.NewGuid():N}.db");
    }

    public static AppDbContext CreateContext(string dbPath, ICurrentTenant currentTenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var context = new AppDbContext(options, currentTenant);
        context.Database.EnsureCreated();
        return context;
    }

    public static void DeleteDatabaseFile(string dbPath)
    {
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TenantFence.EFCore;

namespace TenantFence.EFCore.Tests;

public sealed class AppDbContext : TenantDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options, currentTenant)
    {
    }

    public DbSet<Widget> Widgets => Set<Widget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Widget>(entity =>
        {
            entity.HasKey(widget => widget.Id);
            entity.Property(widget => widget.TenantId).IsRequired();
            entity.Property(widget => widget.Name).IsRequired();
        });
    }
}

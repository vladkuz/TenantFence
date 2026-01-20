using TenantFence.EFCore;

namespace TenantFence.EFCore.Tests;

public sealed class Widget : IMultiTenant
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

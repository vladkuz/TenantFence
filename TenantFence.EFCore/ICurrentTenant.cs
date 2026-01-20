namespace TenantFence.EFCore;

public interface ICurrentTenant
{
    string? Id { get; }
    IDisposable Use(string tenantId);
    void Clear();
}

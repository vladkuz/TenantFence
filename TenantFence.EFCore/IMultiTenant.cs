namespace TenantFence.EFCore;

public interface IMultiTenant
{
    string TenantId { get; set; }
}

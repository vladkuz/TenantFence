using System;

namespace TenantFence.EFCore.Exceptions;

public sealed class TenantMismatchException : Exception
{
    public TenantMismatchException()
        : base("Tenant mismatch detected.")
    {
    }

    public TenantMismatchException(string message)
        : base(message)
    {
    }

    public TenantMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TenantMismatchException(string? entityTenantId, string? currentTenantId)
        : base($"Tenant mismatch. Entity tenant '{Format(entityTenantId)}' does not match current tenant '{Format(currentTenantId)}'.")
    {
    }

    private static string Format(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<null>" : value;
    }
}

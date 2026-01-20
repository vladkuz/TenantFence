using System;

namespace TenantFence.EFCore.Exceptions;

public sealed class TenantContextMissingException : Exception
{
    public TenantContextMissingException()
        : base("Tenant context is required for multi-tenant changes.")
    {
    }

    public TenantContextMissingException(string message)
        : base(message)
    {
    }

    public TenantContextMissingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

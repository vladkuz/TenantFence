using System;
using System.Threading;

namespace TenantFence.EFCore;

public sealed class CurrentTenant : ICurrentTenant
{
    private readonly AsyncLocal<string?> _tenantId = new();

    public string? Id => _tenantId.Value;

    public IDisposable Use(string tenantId)
    {
        var prior = _tenantId.Value;
        _tenantId.Value = tenantId;
        return new RestoreScope(this, prior);
    }

    public void Clear()
    {
        _tenantId.Value = null;
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly CurrentTenant _owner;
        private readonly string? _prior;
        private bool _disposed;

        public RestoreScope(CurrentTenant owner, string? prior)
        {
            _owner = owner;
            _prior = prior;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _owner._tenantId.Value = _prior;
            _disposed = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PolicyEngineDemo.Contracts.Interfaces
{
    public interface ITenantProvider
    {
        string? TenantId();
        string? UserId();
    }
}

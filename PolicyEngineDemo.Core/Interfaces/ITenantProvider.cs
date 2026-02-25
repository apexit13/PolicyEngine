using System;
using System.Collections.Generic;
using System.Text;

namespace PolicyEngineDemo.Core.Interfaces
{
    public interface ITenantProvider
    {
        string? GetTenantId();
        string? GetUserId();
    }
}

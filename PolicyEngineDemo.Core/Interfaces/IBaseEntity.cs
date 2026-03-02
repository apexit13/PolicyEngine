using System;
using System.Collections.Generic;
using System.Text;

namespace PolicyEngineDemo.Contracts.Interfaces
{
    public interface IBaseEntity
    {
        Guid Id { get; set; }
        string TenantId { get; set; } // The Multi-tenant Partition Key
        DateTime CreatedAt { get; set; }
        string CreatedBy { get; set; } // The User ID from the JWT
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PolicyEngineDemo.Shared.Responses
{
    public class PolicyResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
        public string TenantId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
    }
}

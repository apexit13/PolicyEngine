using System;
using System.Collections.Generic;
using System.Text;

namespace PolicyEngineDemo.Contracts.Requests
{
    public class UpdatePolicyRequest
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
    }
}

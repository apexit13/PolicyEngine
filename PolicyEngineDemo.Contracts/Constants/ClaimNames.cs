using System;
using System.Collections.Generic;
using System.Text;

namespace PolicyEngineDemo.Contracts.Constants
{
    public static class ClaimNames
    {
        // Auth0 Post-Login Action injects these into both the access token and ID token.
        public const string Roles = "https://policyengine/roles";
        public const string TenantId = "tid";

        // ASP.NET Core JWT middleware maps 'tid' to this URI automatically.
        // Used server-side in TenantProvider and AuditMiddleware.
        public const string TenantIdMapped =
            "http://schemas.microsoft.com/identity/claims/tenantid";
    }
}

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace PolicyEngineDemo.Web.Models
{
    public class CustomUserAccount : RemoteUserAccount
    {
        [JsonPropertyName("https://policyengine")]
        public string[] Roles { get; set; } = Array.Empty<string>();

        [JsonPropertyName("https://policyengine")]
        public string? TenantId { get; set; }
    }
}

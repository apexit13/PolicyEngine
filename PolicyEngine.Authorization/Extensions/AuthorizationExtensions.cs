using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using PolicyEngine.Authorization.Constants;
using PolicyEngine.Authorization.Handlers;
using PolicyEngine.Authorization.Requirements;
using PolicyEngine.Authorization.Services;

namespace PolicyEngine.Authorization.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
            services.AddScoped<IPermissionService, PermissionService>();

            return services;
        }

        public static AuthorizationOptions AddPermissionPolicies(this AuthorizationOptions options)
        { 
            options.AddPolicy(PolicyNames.ReadPolicies, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.ReadPolicies)));

            options.AddPolicy(PolicyNames.ManagePolicies, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.ManagePolicies)));

            options.AddPolicy(PolicyNames.ReadDashboard, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.ReadDashboard)));

            return options;
        }
    }
}
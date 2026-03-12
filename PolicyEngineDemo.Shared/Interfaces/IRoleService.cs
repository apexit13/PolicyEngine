namespace PolicyEngineDemo.Shared.Interfaces;

/// <summary>
/// Shared role-resolution contract.
/// Web implementation (JWT decode): PolicyEngineDemo.Web.Services.RoleService
/// </summary>
public interface IRoleService
{
    Task<bool> IsAdminAsync();
    Task<IReadOnlyList<string>> GetRolesAsync();
}

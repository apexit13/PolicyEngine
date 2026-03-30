namespace PolicyEngine.Shared.Interfaces
{
    public interface ITenantProvider
    {
        string? TenantId();
        string? UserId();
    }
}

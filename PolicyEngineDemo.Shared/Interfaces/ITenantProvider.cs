namespace PolicyEngineDemo.Shared.Interfaces
{
    public interface ITenantProvider
    {
        string? TenantId();
        string? UserId();
    }
}

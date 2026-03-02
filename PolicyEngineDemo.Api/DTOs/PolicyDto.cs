namespace PolicyEngineDemo.Api.DTOs
{
    public record PolicyDto(string Title, string Description, bool IsActive = true);
}

namespace PolicyEngineDemo.Shared.Requests
{
    public class CreatePolicyRequest
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}

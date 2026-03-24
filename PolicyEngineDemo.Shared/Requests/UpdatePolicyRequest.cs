using System.ComponentModel.DataAnnotations;

namespace PolicyEngineDemo.Shared.Requests;

public class UpdatePolicyRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string Description { get; set; } = "";

    public bool IsActive { get; set; }
}
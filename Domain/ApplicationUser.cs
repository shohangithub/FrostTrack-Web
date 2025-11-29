using Microsoft.AspNetCore.Identity;

namespace Domain;

public class ApplicationUser : IdentityUser<int>
{
    // Additional properties used in the existing domain can be added here
    public string? Name { get; set; }
    public Guid TenantId { get; set; }
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProfileImageUrl { get; set; }
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
}

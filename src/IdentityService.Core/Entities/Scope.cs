namespace IdentityService.Core.Entities;

public class Scope
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!; // e.g., "api:users:read", "api:orders:write"
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Client> Clients { get; set; } = [];
}

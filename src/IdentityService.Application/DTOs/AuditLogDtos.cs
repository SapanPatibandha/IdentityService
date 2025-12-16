namespace IdentityService.Application.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public string Action { get; set; } = null!;
    public string Resource { get; set; } = null!;
    public string? Description { get; set; }
    public string IpAddress { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

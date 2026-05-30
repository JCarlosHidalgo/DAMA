namespace Backend.Dtos.Output;

public class UserClaimsDto
{
    public required string TenantId { get; set; }

    public required string TenantName { get; set; }

    public required string UserId { get; set; }

    public required string UserName { get; set; }

    public required string UserRole { get; set; }
}

namespace Backend.Dtos.Tenants.Input;

public class UpdateTenantTimezoneDto
{
    public required string Timezone { get; set; } = string.Empty;
}

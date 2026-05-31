namespace Backend.Dtos.Tenants.Output;

public class TenantDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Timezone { get; set; } = string.Empty;
}

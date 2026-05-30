using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Tenants;

public class Tenant : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Text(200)]
    public string Name { get; set; } = string.Empty;

    [Text(64)]
    public string Timezone { get; set; } = "America/La_Paz";
}

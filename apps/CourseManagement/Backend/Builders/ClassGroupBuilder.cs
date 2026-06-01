using Backend.Dtos.Groups.Input;
using Backend.Entities.Groups;

namespace Backend.Builders;

public sealed class ClassGroupBuilder : IClassGroupBuilder
{
    public ClassGroup BuildClassGroup(Guid tenantId, CreateClassGroupDto payload)
    {
        return new ClassGroup
        {
            Id = Guid.NewGuid(),
            Name = payload.Name,
            TenantId = tenantId
        };
    }
}

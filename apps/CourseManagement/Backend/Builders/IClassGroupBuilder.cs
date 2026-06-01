using Backend.Dtos.Groups.Input;
using Backend.Entities.Groups;

namespace Backend.Builders;

public interface IClassGroupBuilder
{
    ClassGroup BuildClassGroup(Guid tenantId, CreateClassGroupDto payload);
}

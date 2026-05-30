using Backend.Entities;
using Backend.Entities.Users;

namespace Backend.Builders;

public interface IStudentRegisteredEventBuilder
{
    OutboxEvent Build(User user, Guid tenantId);
}

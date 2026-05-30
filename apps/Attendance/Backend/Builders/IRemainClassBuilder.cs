using Backend.Entities.Remain;

namespace Backend.Builders;

public interface IRemainClassBuilder
{
    StudentRemainClasses BuildEmptyRemain(Guid tenantId, Guid studentId);
}

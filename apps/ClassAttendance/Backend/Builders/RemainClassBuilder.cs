using Backend.Entities.Remain;

namespace Backend.Builders;

public sealed class RemainClassBuilder : IRemainClassBuilder
{
    public StudentRemainClasses BuildEmptyRemain(Guid tenantId, Guid studentId)
    {
        return new StudentRemainClasses
        {
            TenantId = tenantId,
            Id = studentId,
            NumberOfClasses = 0,
            StudentName = null
        };
    }
}

using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Dtos.Remain.Output;
using Backend.Entities.Remain;
using Backend.Results.Remain;
using Backend.Services.Abstract.Remain;

namespace Backend.Services.Concrete.Remain;

public sealed class RemainClassReader(IStudentRemainClassesDao remainClassesDao,
                                       IClaimContext claimContext,
                                       IRemainClassBuilder remainClassBuilder,
                                       IMapper mapper) : IRemainClassReader
{
    public async Task<GetRemainForStudentOutcome> GetForStudentAsync(Guid studentId)
    {
        Guid tenantId = claimContext.TenantId;
        if (claimContext.IsStudentAccessingOtherStudent(studentId))
        {
            return new GetRemainForStudentOutcome.Forbidden();
        }

        StudentRemainClasses remainClasses = await remainClassesDao.ReadAsync(tenantId, studentId)
                                             ?? remainClassBuilder.BuildEmptyRemain(tenantId, studentId);
        return new GetRemainForStudentOutcome.Found(mapper.Map<RemainResponse>(remainClasses));
    }

    public async Task<RemainResponse> GetForCurrentStudentAsync()
    {
        Guid tenantId = claimContext.TenantId;
        Guid studentId = claimContext.UserId;

        StudentRemainClasses remainClasses = await remainClassesDao.ReadAsync(tenantId, studentId)
                                             ?? remainClassBuilder.BuildEmptyRemain(tenantId, studentId);
        return mapper.Map<RemainResponse>(remainClasses);
    }
}

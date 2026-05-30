using Backend.Transporters.Entities;

namespace Backend.Services.Abstract;

public interface ICourseManagementClient
{
    Task<ClassExistenceMeta?> FindScheduledClassAsync(Guid classId, DateOnly classDate);

    Task<ClassExistenceMeta?> FindUniqueClassAsync(Guid classId);
}

using Backend.Services.Abstract;
using Backend.Transporters.Entities;

using DAMA.Software.ValidateCourse.Grpc;

using Grpc.Core;

namespace Backend.Services.Concrete;

public sealed class CourseManagementClient(ClassExistence.ClassExistenceClient grpc) : ICourseManagementClient
{
    public async Task<ClassExistenceMeta?> FindScheduledClassAsync(Guid classId, DateOnly classDate)
    {
        try
        {
            ClassExistsResponse response = await grpc.ScheduledExistsAsync(new ScheduledExistsRequest
            {
                ClassId = classId.ToString(),
                ClassDate = classDate.ToString("yyyy-MM-dd")
            });
            if (!response.Exists)
            {
                return null;
            }

            TimeOnly start = TimeOnly.ParseExact(response.StartTime, "HH:mm:ss");
            TimeOnly end = TimeOnly.ParseExact(response.EndTime, "HH:mm:ss");
            return new ClassExistenceMeta(start, end, ClassDate: null, response.MaxStudentLimit);
        }
        catch (RpcException grpcException)
        {
            throw new HttpRequestException(
                $"ClassExistence.ScheduledExists failed: {grpcException.StatusCode}",
                grpcException);
        }
    }

    public async Task<ClassExistenceMeta?> FindUniqueClassAsync(Guid classId)
    {
        try
        {
            ClassExistsResponse response = await grpc.UniqueExistsAsync(new UniqueExistsRequest
            {
                ClassId = classId.ToString()
            });
            if (!response.Exists)
            {
                return null;
            }

            TimeOnly start = TimeOnly.ParseExact(response.StartTime, "HH:mm:ss");
            TimeOnly end = TimeOnly.ParseExact(response.EndTime, "HH:mm:ss");
            DateOnly date = DateOnly.ParseExact(response.ClassDate, "yyyy-MM-dd");
            return new ClassExistenceMeta(start, end, date, response.MaxStudentLimit);
        }
        catch (RpcException grpcException)
        {
            throw new HttpRequestException(
                $"ClassExistence.UniqueExists failed: {grpcException.StatusCode}",
                grpcException);
        }
    }
}

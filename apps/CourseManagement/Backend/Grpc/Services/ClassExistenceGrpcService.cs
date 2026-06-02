using Backend.Application.Mediator;
using Backend.Application.Scheduleds;
using Backend.Application.Uniques;
using Backend.Results.Scheduleds;
using Backend.Results.Uniques;

using DAMA.Software.GrpcContracts;

using Grpc.Core;

using Microsoft.AspNetCore.Authorization;

namespace Backend.Grpc.Services;

[Authorize]
public sealed class ClassExistenceGrpcService(
    IQueryHandler<FindScheduledClassQuery, FindScheduledClassResult> findScheduledHandler,
    IQueryHandler<FindUniqueClassQuery, FindUniqueClassResult> findUniqueHandler)
    : ClassExistence.ClassExistenceBase
{
    public override async Task<ClassExistsResponse> ScheduledExists(ScheduledExistsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ClassId, out Guid classId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "class_id is not a valid GUID."));
        }

        if (!DateOnly.TryParseExact(request.ClassDate, "yyyy-MM-dd", out DateOnly classDate))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "class_date must be yyyy-MM-dd."));
        }

        FindScheduledClassResult result = await findScheduledHandler.Handle(new FindScheduledClassQuery(classId, classDate));
        return result switch
        {
            FindScheduledClassResult.NotFound => new ClassExistsResponse { Exists = false },
            FindScheduledClassResult.Found found => new ClassExistsResponse
            {
                Exists = true,
                StartTime = found.Meta.StartTime.ToString("HH:mm:ss"),
                EndTime = found.Meta.EndTime.ToString("HH:mm:ss"),
                ClassDate = string.Empty,
                MaxStudentLimit = found.Meta.MaxStudentLimit
            },
            _ => throw new System.Diagnostics.UnreachableException()
        };
    }

    public override async Task<ClassExistsResponse> UniqueExists(UniqueExistsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ClassId, out Guid classId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "class_id is not a valid GUID."));
        }

        FindUniqueClassResult result = await findUniqueHandler.Handle(new FindUniqueClassQuery(classId));
        return result switch
        {
            FindUniqueClassResult.NotFound => new ClassExistsResponse { Exists = false },
            FindUniqueClassResult.Found found => new ClassExistsResponse
            {
                Exists = true,
                StartTime = found.Meta.StartTime.ToString("HH:mm:ss"),
                EndTime = found.Meta.EndTime.ToString("HH:mm:ss"),
                ClassDate = found.Meta.ClassDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                MaxStudentLimit = found.Meta.MaxStudentLimit
            },
            _ => throw new System.Diagnostics.UnreachableException()
        };
    }
}

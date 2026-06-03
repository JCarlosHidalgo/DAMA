using System.Diagnostics;

using Backend.Application.Courses;
using Backend.Application.Mediator;
using Backend.Results.Courses;
using Backend.Security;

using DAMA.Software.GrpcContracts;

using Grpc.Core;

using Microsoft.AspNetCore.Authorization;

namespace Backend.Grpc.Services;

[Authorize(Roles = UserRoles.Client)]
public sealed class CourseExistenceGrpcService(
    IQueryHandler<CourseExistsQuery, CourseExistsResult> courseExistsHandler)
    : CourseExistence.CourseExistenceBase
{
    public override async Task<CourseExistsResponse> Exists(CourseExistsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CourseId, out Guid courseId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "course_id is not a valid GUID."));
        }

        CourseExistsResult result = await courseExistsHandler.Handle(new CourseExistsQuery(courseId));
        return result switch
        {
            CourseExistsResult.Exists => new CourseExistsResponse { Exists = true },
            CourseExistsResult.DoesNotExist => new CourseExistsResponse { Exists = false },
            _ => throw new UnreachableException()
        };
    }
}

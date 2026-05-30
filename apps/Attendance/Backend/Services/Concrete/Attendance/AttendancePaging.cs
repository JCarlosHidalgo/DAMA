using AutoMapper;

using Backend.Builders;
using Backend.Common;

namespace Backend.Services.Concrete.Attendance;

internal static class AttendancePaging
{
    public static async Task<PageDto<TResponse>> BuildPageAsync<TEntity, TResponse>(
        int pageIndex,
        int pageSize,
        Func<Task<int>> countAsync,
        Func<int, int, Task<List<TEntity>>> getPageAsync,
        IMapper mapper,
        IAttendanceClassBuilder attendanceClassBuilder)
    {
        int totalCount = await countAsync();
        int maxIndex = Pagination.ComputeMaxIndex(totalCount, pageSize);

        List<TEntity> attendancePage = pageIndex > maxIndex
            ? new List<TEntity>()
            : await getPageAsync(pageIndex * pageSize, pageSize);

        List<TResponse> responsePage = mapper.Map<List<TResponse>>(attendancePage);
        return attendanceClassBuilder.BuildPage(pageIndex, maxIndex, responsePage);
    }
}

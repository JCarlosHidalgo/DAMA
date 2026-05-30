using Backend.Common;
using Backend.Dtos.Attendance.Input;
using Backend.Entities.Attendance;
using Backend.Transporters.Entities;

namespace Backend.Builders;

public interface IAttendanceClassBuilder
{
    ScheduledClassAttendance BuildScheduledAttendance(Guid tenantId, Guid studentId, string studentName, DateOnly classDate, ScheduledAttendanceDto request, ClassExistenceMeta metadata);

    UniqueClassAttendance BuildUniqueAttendance(Guid tenantId, Guid studentId, string studentName, DateOnly classDate, UniqueAttendanceDto request, ClassExistenceMeta metadata);

    PageDto<TItem> BuildPage<TItem>(int currentIndex, int maxIndex, List<TItem> items);
}

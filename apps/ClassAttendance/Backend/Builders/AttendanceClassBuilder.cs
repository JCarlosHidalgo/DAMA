using Backend.Common;
using Backend.Dtos.Attendance.Input;
using Backend.Entities.Attendance;
using Backend.Transporters.Entities;

namespace Backend.Builders;

public sealed class AttendanceClassBuilder : IAttendanceClassBuilder
{
    public ScheduledClassAttendance BuildScheduledAttendance(Guid tenantId, Guid studentId, string studentName, DateOnly classDate, ScheduledAttendanceDto request, ClassExistenceMeta metadata)
    {
        return new ScheduledClassAttendance
        {
            TenantId = tenantId,
            ClassId = request.ClassId,
            ClassDate = classDate,
            StartTime = metadata.StartTime,
            EndTime = metadata.EndTime,
            CourseName = request.CourseName,
            StudentId = studentId,
            StudentName = studentName
        };
    }

    public UniqueClassAttendance BuildUniqueAttendance(Guid tenantId, Guid studentId, string studentName, DateOnly classDate, UniqueAttendanceDto request, ClassExistenceMeta metadata)
    {
        return new UniqueClassAttendance
        {
            TenantId = tenantId,
            ClassId = request.ClassId,
            ClassDate = classDate,
            StartTime = metadata.StartTime,
            EndTime = metadata.EndTime,
            CourseName = request.CourseName,
            StudentId = studentId,
            StudentName = studentName
        };
    }

    public PageDto<TItem> BuildPage<TItem>(int currentIndex, int maxIndex, List<TItem> items)
    {
        return new PageDto<TItem>
        {
            CurrentIndex = currentIndex,
            MaxIndex = maxIndex,
            Items = items
        };
    }
}

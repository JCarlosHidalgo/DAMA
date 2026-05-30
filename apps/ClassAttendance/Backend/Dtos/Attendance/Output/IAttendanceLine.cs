namespace Backend.Dtos.Attendance.Output;

public interface IAttendanceLine
{
    Guid ClassId { get; }

    DateOnly ClassDate { get; }

    TimeOnly StartTime { get; }

    TimeOnly EndTime { get; }

    string CourseName { get; }

    Guid StudentId { get; }

    string StudentName { get; }
}

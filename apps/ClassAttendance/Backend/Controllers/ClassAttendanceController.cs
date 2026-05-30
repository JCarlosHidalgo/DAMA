using System.Diagnostics;

using Backend.Common;
using Backend.Dtos.Attendance.Input;
using Backend.Dtos.Attendance.Output;
using Backend.Dtos.Remain.Input;
using Backend.Dtos.Remain.Output;
using Backend.Entities.Users;
using Backend.Options;
using Backend.Results.Attendance;
using Backend.Results.Remain;
using Backend.Services.Abstract.Attendance;
using Backend.Services.Abstract.Remain;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

[Route("api/class-attendance")]
[ApiController]
public class ClassAttendanceController : ControllerBase
{
    private readonly IScheduledClassService _scheduledService;
    private readonly IUniqueClassService _uniqueService;
    private readonly IRemainClassReader _remainReader;
    private readonly IRemainClassWriter _remainWriter;
    private readonly AttendanceOptions _attendanceOptions;

    public ClassAttendanceController(IScheduledClassService scheduledService,
                                     IUniqueClassService uniqueService,
                                     IRemainClassReader remainReader,
                                     IRemainClassWriter remainWriter,
                                     IOptions<AttendanceOptions> attendanceOptions)
    {
        _scheduledService = scheduledService;
        _uniqueService = uniqueService;
        _remainReader = remainReader;
        _remainWriter = remainWriter;
        _attendanceOptions = attendanceOptions.Value;
    }

    [Authorize(Roles = UserRoles.ClientOrTeacher)]
    [HttpGet("attendance/scheduled/class/{classId}/{currenDate}")]
    public async Task<ActionResult> GetScheduledClassAttendance(Guid classId, DateOnly currenDate)
    {
        List<ScheduledAttendanceResponse> scheduledAttendance =
            await _scheduledService.GetScheduledAttendance(classId, currenDate);
        return Ok(scheduledAttendance);
    }

    [Authorize]
    [HttpGet("attendance/scheduled/student/{studentId}")]
    public async Task<ActionResult> GetScheduledClassAttendance(Guid studentId)
    {
        GetScheduledByStudentOutcome outcome =
            await _scheduledService.GetScheduledAttendanceByStudentId(studentId);
        return outcome switch
        {
            GetScheduledByStudentOutcome.Found found => Ok(found.Attendances),
            GetScheduledByStudentOutcome.Forbidden => Forbid(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet("attendance/scheduled/me")]
    public async Task<ActionResult> ListMyScheduledAttendance([FromQuery] PaginationParamsDto pagination)
    {
        var page = await _scheduledService.ListMyScheduledAttendanceAsync(pagination.Index);
        return Ok(page);
    }

    [Authorize(Roles = UserRoles.ClientOrTeacher)]
    [HttpGet("attendance/unique/class/{classId}")]
    public async Task<ActionResult> GetUniqueClassAttendance(Guid classId)
    {
        List<UniqueAttendanceResponse> uniqueAttendance = await _uniqueService.GetUniqueAttendance(classId);
        return Ok(uniqueAttendance);
    }

    [Authorize]
    [HttpGet("attendance/unique/student/{studentId}")]
    public async Task<ActionResult> GetStudentAttendance(Guid studentId)
    {
        GetUniqueByStudentOutcome outcome =
            await _uniqueService.GetUniqueAttendanceByStudentId(studentId);
        return outcome switch
        {
            GetUniqueByStudentOutcome.Found found => Ok(found.Attendances),
            GetUniqueByStudentOutcome.Forbidden => Forbid(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet("attendance/unique/me")]
    public async Task<ActionResult> ListMyUniqueAttendance([FromQuery] PaginationParamsDto pagination)
    {
        var page = await _uniqueService.ListMyUniqueAttendanceAsync(pagination.Index);
        return Ok(page);
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpPost("attendance/scheduled")]
    public async Task<ActionResult> MarkScheduledClassAttendance(ScheduledAttendanceDto scheduledAttendanceRequest)
    {
        MarkAttendanceOutcome outcome = await _scheduledService.MarkScheduledAttendance(scheduledAttendanceRequest);
        return MapMarkAttendanceOutcome(outcome);
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpPost("attendance/unique")]
    public async Task<ActionResult> MarkUniqueClassAttendance(UniqueAttendanceDto uniqueAttendanceRequest)
    {
        MarkAttendanceOutcome outcome = await _uniqueService.MarkUniqueAttendance(uniqueAttendanceRequest);
        return MapMarkAttendanceOutcome(outcome);
    }

    [Authorize]
    [HttpGet("remain/me")]
    public async Task<ActionResult> GetMyRemainClasses()
    {
        RemainResponse remain = await _remainReader.GetForCurrentStudentAsync();
        return Ok(remain);
    }

    [Authorize]
    [HttpGet("remain/{studentId}")]
    public async Task<ActionResult> GetStudentRemainClasses(Guid studentId)
    {
        GetRemainForStudentOutcome outcome = await _remainReader.GetForStudentAsync(studentId);
        return outcome switch
        {
            GetRemainForStudentOutcome.Found found => Ok(found.Remain),
            GetRemainForStudentOutcome.Forbidden => Forbid(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPost("remain/client/{studentId}")]
    public async Task<ActionResult> ClientIncrementStudentRemain(
        Guid studentId,
        IncrementStudentRemainDto incrementRequest)
    {
        IncrementStudentRemainOutcome outcome = await _remainWriter.IncrementForStudentByClientAsync(
            incrementRequest.RequestId,
            studentId,
            incrementRequest.Quantity,
            incrementRequest.StudentName);
        return outcome switch
        {
            IncrementStudentRemainOutcome.Applied => Ok(),
            IncrementStudentRemainOutcome.AlreadyApplied => Ok(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPost("remain/client/tenant")]
    public async Task<ActionResult> ClientIncrementAllInTenant(IncrementTenantRemainDto incrementRequest)
    {
        IncrementTenantRemainOutcome outcome = await _remainWriter.IncrementAllInTenantByClientAsync(
            incrementRequest.RequestId,
            incrementRequest.Quantity);
        return outcome switch
        {
            IncrementTenantRemainOutcome.Applied applied => Ok(new { affected = applied.Affected, replayed = false }),
            IncrementTenantRemainOutcome.AlreadyApplied => Ok(new { affected = 0, replayed = true }),
            _ => throw new UnreachableException()
        };
    }

    private ActionResult MapMarkAttendanceOutcome(MarkAttendanceOutcome outcome)
    {
        return outcome switch
        {
            MarkAttendanceOutcome.Marked => Ok(),
            MarkAttendanceOutcome.AlreadyMarked => Conflict("Asistencia ya registrada."),
            MarkAttendanceOutcome.NoRemainingClasses => BadRequest("Sin clases restantes."),
            MarkAttendanceOutcome.InvalidClass => NotFound("Clase no existe para este tenant."),
            MarkAttendanceOutcome.ClassFull => Conflict("Clase llena: se alcanzó el límite de estudiantes."),
            MarkAttendanceOutcome.OutsideAllowedWindow => BadRequest(
                $"Fuera del horario permitido ({_attendanceOptions.AllowedWindowStart:HH\\:mm}-{_attendanceOptions.AllowedWindowEnd:HH\\:mm})."),
            MarkAttendanceOutcome.InvalidTenantTimezone => StatusCode(
                StatusCodes.Status500InternalServerError,
                "Configuración de zona horaria del tenant inválida."),
            _ => throw new UnreachableException()
        };
    }
}

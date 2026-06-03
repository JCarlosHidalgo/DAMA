using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.Common;
using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.Dtos.Attendance.Input;
using Backend.Dtos.Attendance.Output;
using Backend.Entities.Attendance;
using Backend.Options;
using Backend.Results.Attendance;
using Backend.Security;
using Backend.Services.Abstract;
using Backend.Services.Abstract.Attendance;
using Backend.Services.Concrete.Attendance;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Test.Services.Concrete.Attendance;

[TestFixture]
public class ScheduledClassServiceTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CallerStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IScheduledClassAttendanceDao> _scheduledClassAttendanceDao = null!;
    private Mock<ICourseManagementClient> _courseManagementClient = null!;
    private Mock<IAttendanceMarker> _attendanceMarker = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IAttendanceClassBuilder> _attendanceClassBuilder = null!;
    private Mock<IMapper> _mapper = null!;

    private ScheduledClassService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassAttendanceDao = new Mock<IScheduledClassAttendanceDao>(MockBehavior.Strict);
        _courseManagementClient = new Mock<ICourseManagementClient>(MockBehavior.Strict);
        _attendanceMarker = new Mock<IAttendanceMarker>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _attendanceClassBuilder = new Mock<IAttendanceClassBuilder>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);

        _sut = new ScheduledClassService(
            _scheduledClassAttendanceDao.Object,
            _courseManagementClient.Object,
            _attendanceMarker.Object,
            _claimContext.Object,
            Options.Create(new AttendanceOptions { PageSize = 10 }),
            _attendanceClassBuilder.Object,
            _mapper.Object,
            NullLogger<ScheduledClassService>.Instance);
    }

    [Test]
    public async Task GetScheduledAttendance_ReturnsMappedResponses()
    {
        var classId = Guid.NewGuid();
        DateOnly classDate = new(2026, 5, 24);
        List<ScheduledClassAttendance> attendances = [new ScheduledClassAttendance()];
        List<ScheduledAttendanceResponse> mapped = [new ScheduledAttendanceResponse()];

        _scheduledClassAttendanceDao
            .Setup(target => target.GetScheduledAttendanceAsync(CallerTenantId, classId, classDate))
            .ReturnsAsync(attendances);
        _mapper.Setup(target => target.Map<List<ScheduledAttendanceResponse>>(attendances)).Returns(mapped);

        List<ScheduledAttendanceResponse> result = await _sut.GetScheduledAttendance(classId, classDate);

        Assert.That(result, Is.SameAs(mapped));
    }

    [Test]
    public async Task GetScheduledAttendanceByStudentId_WhenStudentAccessingOther_ReturnsForbidden()
    {
        var studentId = Guid.NewGuid();
        _claimContext.Setup(target => target.Role).Returns(UserRoles.Student);
        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);

        GetScheduledByStudentOutcome result = await _sut.GetScheduledAttendanceByStudentId(studentId);

        Assert.That(result, Is.InstanceOf<GetScheduledByStudentOutcome.Forbidden>());
    }

    [Test]
    public async Task GetScheduledAttendanceByStudentId_WhenAllowed_ReturnsFoundWithAttendances()
    {
        var studentId = Guid.NewGuid();
        List<ScheduledClassAttendance> attendances = [new ScheduledClassAttendance()];
        List<ScheduledAttendanceResponse> mapped = [new ScheduledAttendanceResponse()];

        _claimContext.Setup(target => target.Role).Returns(UserRoles.Client);
        _scheduledClassAttendanceDao
            .Setup(target => target.GetScheduledAttendanceByStudentIdAsync(CallerTenantId, studentId))
            .ReturnsAsync(attendances);
        _mapper.Setup(target => target.Map<List<ScheduledAttendanceResponse>>(attendances)).Returns(mapped);

        GetScheduledByStudentOutcome result = await _sut.GetScheduledAttendanceByStudentId(studentId);

        Assert.That(result, Is.InstanceOf<GetScheduledByStudentOutcome.Found>());
        var found = (GetScheduledByStudentOutcome.Found)result;
        Assert.That(found.Attendances, Is.SameAs(mapped));
    }

    [Test]
    public async Task ListMyScheduledAttendanceAsync_BuildsPageFromDaoCountAndPage()
    {
        const int pageIndex = 0;
        List<ScheduledClassAttendance> attendances = [new ScheduledClassAttendance()];
        List<ScheduledAttendanceResponse> mapped = [new ScheduledAttendanceResponse()];
        PageDto<ScheduledAttendanceResponse> expectedPage = new()
        {
            CurrentIndex = 0,
            MaxIndex = 0,
            Items = mapped
        };

        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _scheduledClassAttendanceDao
            .Setup(target => target.CountByStudentForTenantAsync(CallerTenantId, CallerStudentId))
            .ReturnsAsync(1);
        _scheduledClassAttendanceDao
            .Setup(target => target.GetPageByStudentForTenantAsync(CallerTenantId, CallerStudentId, 0, 10))
            .ReturnsAsync(attendances);
        _mapper.Setup(target => target.Map<List<ScheduledAttendanceResponse>>(attendances)).Returns(mapped);
        _attendanceClassBuilder
            .Setup(target => target.BuildPage(pageIndex, 0, mapped))
            .Returns(expectedPage);

        PageDto<ScheduledAttendanceResponse> result = await _sut.ListMyScheduledAttendanceAsync(pageIndex);

        Assert.That(result, Is.SameAs(expectedPage));
    }

    [Test]
    public async Task MarkScheduledAttendance_DelegatesToAttendanceMarker()
    {
        ScheduledAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };

        _attendanceMarker
            .Setup(target => target.MarkAsync<ScheduledClassAttendance, ScheduledAttendanceResponse>(
                It.IsAny<Func<AttendanceMarkContext, Task<AttendanceBuildResult<ScheduledClassAttendance>?>>>(),
                It.IsAny<Func<ScheduledClassAttendance, ITransactionContext, Task<int>>>(),
                It.IsAny<Func<ScheduledClassAttendance, ITransactionContext, Task<bool>>>(),
                It.IsAny<Func<ScheduledClassAttendance, string>>()))
            .ReturnsAsync(new MarkAttendanceOutcome.Marked());

        MarkAttendanceOutcome result = await _sut.MarkScheduledAttendance(request);

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.Marked>());
    }

    [Test]
    public async Task MarkScheduledAttendance_WhenClassFound_BuildsAttendanceWithLocalDate()
    {
        ScheduledAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        ClassExistenceMeta classMetadata = new(new TimeOnly(8, 0), new TimeOnly(10, 0), null, 0);
        ScheduledClassAttendance builtAttendance = new();
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "America/La_Paz");

        _courseManagementClient
            .Setup(target => target.FindScheduledClassAsync(request.ClassId, It.IsAny<DateOnly>()))
            .ReturnsAsync(classMetadata);
        _attendanceClassBuilder
            .Setup(target => target.BuildScheduledAttendance(
                CallerTenantId, CallerStudentId, "Pedro", It.IsAny<DateOnly>(), request, classMetadata))
            .Returns(builtAttendance);

        ScheduledClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.SameAs(builtAttendance));
        _courseManagementClient.Verify(
            target => target.FindScheduledClassAsync(request.ClassId, It.IsAny<DateOnly>()),
            Times.Once);
        _attendanceClassBuilder.Verify(
            target => target.BuildScheduledAttendance(
                CallerTenantId, CallerStudentId, "Pedro", It.IsAny<DateOnly>(), request, classMetadata),
            Times.Once);
    }

    [Test]
    public async Task MarkScheduledAttendance_WhenClassNotFound_ReturnsNullWithoutCallingBuilder()
    {
        ScheduledAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "America/La_Paz");

        _courseManagementClient
            .Setup(target => target.FindScheduledClassAsync(request.ClassId, It.IsAny<DateOnly>()))
            .ReturnsAsync((ClassExistenceMeta?)null);

        ScheduledClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.Null);
        _attendanceClassBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task MarkScheduledAttendance_WhenTimezoneInvalid_LocalDateFallsBackToUtc()
    {
        ScheduledAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        ClassExistenceMeta classMetadata = new(new TimeOnly(8, 0), new TimeOnly(10, 0), null, 0);
        var utcDateToday = DateOnly.FromDateTime(DateTime.UtcNow);
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "Continent/NonExistent");

        _courseManagementClient
            .Setup(target => target.FindScheduledClassAsync(request.ClassId, utcDateToday))
            .ReturnsAsync(classMetadata);
        _attendanceClassBuilder
            .Setup(target => target.BuildScheduledAttendance(
                CallerTenantId, CallerStudentId, "Pedro", utcDateToday, request, classMetadata))
            .Returns(new ScheduledClassAttendance());

        await InvokeMarkLambdaAsync(request, markContext);

        _courseManagementClient.Verify(
            target => target.FindScheduledClassAsync(request.ClassId, utcDateToday),
            Times.Once);
    }

    [Test]
    public async Task ListMyScheduledAttendanceAsync_WhenPageIndexBeyondMax_SkipsGetPage()
    {
        const int pageIndexBeyondMax = 5;
        List<ScheduledAttendanceResponse> emptyMapped = [];
        PageDto<ScheduledAttendanceResponse> expectedPage = new()
        {
            CurrentIndex = pageIndexBeyondMax,
            MaxIndex = 0,
            Items = emptyMapped
        };

        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _scheduledClassAttendanceDao
            .Setup(target => target.CountByStudentForTenantAsync(CallerTenantId, CallerStudentId))
            .ReturnsAsync(1);
        _mapper
            .Setup(target => target.Map<List<ScheduledAttendanceResponse>>(
                It.Is<List<ScheduledClassAttendance>>(list => list.Count == 0)))
            .Returns(emptyMapped);
        _attendanceClassBuilder
            .Setup(target => target.BuildPage(pageIndexBeyondMax, 0, emptyMapped))
            .Returns(expectedPage);

        PageDto<ScheduledAttendanceResponse> result = await _sut.ListMyScheduledAttendanceAsync(pageIndexBeyondMax);

        Assert.That(result, Is.SameAs(expectedPage));
        _scheduledClassAttendanceDao.Verify(
            target => target.GetPageByStudentForTenantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    private async Task<ScheduledClassAttendance?> InvokeMarkLambdaAsync(
        ScheduledAttendanceDto request,
        AttendanceMarkContext markContext)
    {
        ScheduledClassAttendance? capturedAttendance = null;
        _attendanceMarker
            .Setup(target => target.MarkAsync<ScheduledClassAttendance, ScheduledAttendanceResponse>(
                It.IsAny<Func<AttendanceMarkContext, Task<AttendanceBuildResult<ScheduledClassAttendance>?>>>(),
                It.IsAny<Func<ScheduledClassAttendance, ITransactionContext, Task<int>>>(),
                It.IsAny<Func<ScheduledClassAttendance, ITransactionContext, Task<bool>>>(),
                It.IsAny<Func<ScheduledClassAttendance, string>>()))
            .Returns(async (
                Func<AttendanceMarkContext, Task<AttendanceBuildResult<ScheduledClassAttendance>?>> resolveAndBuild,
                Func<ScheduledClassAttendance, ITransactionContext, Task<int>> countOtherStudents,
                Func<ScheduledClassAttendance, ITransactionContext, Task<bool>> tryMark,
                Func<ScheduledClassAttendance, string> resolveGroup) =>
            {
                AttendanceBuildResult<ScheduledClassAttendance>? buildResult = await resolveAndBuild(markContext);
                capturedAttendance = buildResult?.Attendance;
                if (capturedAttendance is not null)
                {
                    _scheduledClassAttendanceDao
                        .Setup(target => target.TryMarkAttendanceAsync(capturedAttendance, It.IsAny<ITransactionContext>()))
                        .ReturnsAsync(true);
                    await tryMark(capturedAttendance, Mock.Of<ITransactionContext>());
                    resolveGroup(capturedAttendance);
                }
                return capturedAttendance is null
                    ? (MarkAttendanceOutcome)new MarkAttendanceOutcome.InvalidClass()
                    : new MarkAttendanceOutcome.Marked();
            });

        await _sut.MarkScheduledAttendance(request);
        return capturedAttendance;
    }
}

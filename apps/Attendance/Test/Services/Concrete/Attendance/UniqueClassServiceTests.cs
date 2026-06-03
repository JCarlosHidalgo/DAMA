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

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Services.Concrete.Attendance;

[TestFixture]
public class UniqueClassServiceTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CallerStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IUniqueClassAttendanceDao> _uniqueClassAttendanceDao = null!;
    private Mock<ICourseManagementClient> _courseManagementClient = null!;
    private Mock<IAttendanceMarker> _attendanceMarker = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IAttendanceClassBuilder> _attendanceClassBuilder = null!;
    private Mock<IMapper> _mapper = null!;

    private UniqueClassService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _uniqueClassAttendanceDao = new Mock<IUniqueClassAttendanceDao>(MockBehavior.Strict);
        _courseManagementClient = new Mock<ICourseManagementClient>(MockBehavior.Strict);
        _attendanceMarker = new Mock<IAttendanceMarker>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _attendanceClassBuilder = new Mock<IAttendanceClassBuilder>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);

        _sut = new UniqueClassService(
            _uniqueClassAttendanceDao.Object,
            _courseManagementClient.Object,
            _attendanceMarker.Object,
            _claimContext.Object,
            Options.Create(new AttendanceOptions { PageSize = 10 }),
            _attendanceClassBuilder.Object,
            _mapper.Object);
    }

    [Test]
    public async Task GetUniqueAttendance_ReturnsMappedResponses()
    {
        var classId = Guid.NewGuid();
        List<UniqueClassAttendance> attendances = [new UniqueClassAttendance()];
        List<UniqueAttendanceResponse> mapped = [new UniqueAttendanceResponse()];

        _uniqueClassAttendanceDao
            .Setup(target => target.GetUniqueAttendanceAsync(CallerTenantId, classId))
            .ReturnsAsync(attendances);
        _mapper.Setup(target => target.Map<List<UniqueAttendanceResponse>>(attendances)).Returns(mapped);

        List<UniqueAttendanceResponse> result = await _sut.GetUniqueAttendance(classId);

        Assert.That(result, Is.SameAs(mapped));
    }

    [Test]
    public async Task GetUniqueAttendanceByStudentId_WhenStudentAccessingOther_ReturnsForbidden()
    {
        var studentId = Guid.NewGuid();
        _claimContext.Setup(target => target.Role).Returns(UserRoles.Student);
        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);

        GetUniqueByStudentOutcome result = await _sut.GetUniqueAttendanceByStudentId(studentId);

        Assert.That(result, Is.InstanceOf<GetUniqueByStudentOutcome.Forbidden>());
    }

    [Test]
    public async Task GetUniqueAttendanceByStudentId_WhenAllowed_ReturnsFoundWithAttendances()
    {
        var studentId = Guid.NewGuid();
        List<UniqueClassAttendance> attendances = [new UniqueClassAttendance()];
        List<UniqueAttendanceResponse> mapped = [new UniqueAttendanceResponse()];

        _claimContext.Setup(target => target.Role).Returns(UserRoles.Client);
        _uniqueClassAttendanceDao
            .Setup(target => target.GetUniqueAttendanceByStudentIdAsync(CallerTenantId, studentId))
            .ReturnsAsync(attendances);
        _mapper.Setup(target => target.Map<List<UniqueAttendanceResponse>>(attendances)).Returns(mapped);

        GetUniqueByStudentOutcome result = await _sut.GetUniqueAttendanceByStudentId(studentId);

        Assert.That(result, Is.InstanceOf<GetUniqueByStudentOutcome.Found>());
    }

    [Test]
    public async Task ListMyUniqueAttendanceAsync_BuildsPage()
    {
        List<UniqueClassAttendance> attendances = [new UniqueClassAttendance()];
        List<UniqueAttendanceResponse> mapped = [new UniqueAttendanceResponse()];
        PageDto<UniqueAttendanceResponse> expectedPage = new() { CurrentIndex = 0, MaxIndex = 0, Items = mapped };

        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _uniqueClassAttendanceDao
            .Setup(target => target.CountByStudentForTenantAsync(CallerTenantId, CallerStudentId))
            .ReturnsAsync(1);
        _uniqueClassAttendanceDao
            .Setup(target => target.GetPageByStudentForTenantAsync(CallerTenantId, CallerStudentId, 0, 10))
            .ReturnsAsync(attendances);
        _mapper.Setup(target => target.Map<List<UniqueAttendanceResponse>>(attendances)).Returns(mapped);
        _attendanceClassBuilder.Setup(target => target.BuildPage(0, 0, mapped)).Returns(expectedPage);

        PageDto<UniqueAttendanceResponse> result = await _sut.ListMyUniqueAttendanceAsync(0);

        Assert.That(result, Is.SameAs(expectedPage));
    }

    [Test]
    public async Task MarkUniqueAttendance_DelegatesToAttendanceMarker()
    {
        UniqueAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };

        _attendanceMarker
            .Setup(target => target.MarkAsync<UniqueClassAttendance, UniqueAttendanceResponse>(
                It.IsAny<Func<AttendanceMarkContext, Task<AttendanceBuildResult<UniqueClassAttendance>?>>>(),
                It.IsAny<Func<UniqueClassAttendance, ITransactionContext, Task<int>>>(),
                It.IsAny<Func<UniqueClassAttendance, ITransactionContext, Task<bool>>>(),
                It.IsAny<Func<UniqueClassAttendance, string>>()))
            .ReturnsAsync(new MarkAttendanceOutcome.Marked());

        MarkAttendanceOutcome result = await _sut.MarkUniqueAttendance(request);

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.Marked>());
    }

    [Test]
    public async Task MarkUniqueAttendance_WhenClassFoundWithDate_BuildsAttendanceWithMetadataDate()
    {
        UniqueAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        DateOnly metadataDate = new(2026, 5, 24);
        ClassExistenceMeta classMetadata = new(new TimeOnly(8, 0), new TimeOnly(10, 0), metadataDate, 0);
        UniqueClassAttendance builtAttendance = new();
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "America/La_Paz");

        _courseManagementClient
            .Setup(target => target.FindUniqueClassAsync(request.ClassId))
            .ReturnsAsync(classMetadata);
        _attendanceClassBuilder
            .Setup(target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", metadataDate, request, classMetadata))
            .Returns(builtAttendance);

        UniqueClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.SameAs(builtAttendance));
        _attendanceClassBuilder.Verify(
            target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", metadataDate, request, classMetadata),
            Times.Once);
    }

    [Test]
    public async Task MarkUniqueAttendance_WhenMetadataDateNull_FallsBackToUtcToday()
    {
        UniqueAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        ClassExistenceMeta classMetadata = new(new TimeOnly(8, 0), new TimeOnly(10, 0), null, 0);
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        UniqueClassAttendance builtAttendance = new();
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "America/La_Paz");

        _courseManagementClient
            .Setup(target => target.FindUniqueClassAsync(request.ClassId))
            .ReturnsAsync(classMetadata);
        _attendanceClassBuilder
            .Setup(target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", utcToday, request, classMetadata))
            .Returns(builtAttendance);

        UniqueClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.SameAs(builtAttendance));
        _attendanceClassBuilder.Verify(
            target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", utcToday, request, classMetadata),
            Times.Once);
    }

    [Test]
    public async Task MarkUniqueAttendance_WhenClassNotFound_ReturnsNullWithoutCallingBuilder()
    {
        UniqueAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "America/La_Paz");

        _courseManagementClient
            .Setup(target => target.FindUniqueClassAsync(request.ClassId))
            .ReturnsAsync((ClassExistenceMeta?)null);

        UniqueClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.Null);
        _attendanceClassBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ListMyUniqueAttendanceAsync_WhenPageIndexBeyondMax_SkipsGetPage()
    {
        const int pageIndexBeyondMax = 5;
        List<UniqueAttendanceResponse> emptyMapped = [];
        PageDto<UniqueAttendanceResponse> expectedPage = new()
        {
            CurrentIndex = pageIndexBeyondMax,
            MaxIndex = 0,
            Items = emptyMapped
        };

        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _uniqueClassAttendanceDao
            .Setup(target => target.CountByStudentForTenantAsync(CallerTenantId, CallerStudentId))
            .ReturnsAsync(1);
        _mapper
            .Setup(target => target.Map<List<UniqueAttendanceResponse>>(
                It.Is<List<UniqueClassAttendance>>(list => list.Count == 0)))
            .Returns(emptyMapped);
        _attendanceClassBuilder
            .Setup(target => target.BuildPage(pageIndexBeyondMax, 0, emptyMapped))
            .Returns(expectedPage);

        PageDto<UniqueAttendanceResponse> result = await _sut.ListMyUniqueAttendanceAsync(pageIndexBeyondMax);

        Assert.That(result, Is.SameAs(expectedPage));
        _uniqueClassAttendanceDao.Verify(
            target => target.GetPageByStudentForTenantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    private async Task<UniqueClassAttendance?> InvokeMarkLambdaAsync(
        UniqueAttendanceDto request,
        AttendanceMarkContext markContext)
    {
        UniqueClassAttendance? capturedAttendance = null;
        _attendanceMarker
            .Setup(target => target.MarkAsync<UniqueClassAttendance, UniqueAttendanceResponse>(
                It.IsAny<Func<AttendanceMarkContext, Task<AttendanceBuildResult<UniqueClassAttendance>?>>>(),
                It.IsAny<Func<UniqueClassAttendance, ITransactionContext, Task<int>>>(),
                It.IsAny<Func<UniqueClassAttendance, ITransactionContext, Task<bool>>>(),
                It.IsAny<Func<UniqueClassAttendance, string>>()))
            .Returns(async (
                Func<AttendanceMarkContext, Task<AttendanceBuildResult<UniqueClassAttendance>?>> resolveAndBuild,
                Func<UniqueClassAttendance, ITransactionContext, Task<int>> countOtherStudents,
                Func<UniqueClassAttendance, ITransactionContext, Task<bool>> tryMark,
                Func<UniqueClassAttendance, string> resolveGroup) =>
            {
                AttendanceBuildResult<UniqueClassAttendance>? buildResult = await resolveAndBuild(markContext);
                capturedAttendance = buildResult?.Attendance;
                if (capturedAttendance is not null)
                {
                    _uniqueClassAttendanceDao
                        .Setup(target => target.TryMarkAttendanceAsync(capturedAttendance, It.IsAny<ITransactionContext>()))
                        .ReturnsAsync(true);
                    await tryMark(capturedAttendance, Mock.Of<ITransactionContext>());
                    resolveGroup(capturedAttendance);
                }
                return capturedAttendance is null
                    ? (MarkAttendanceOutcome)new MarkAttendanceOutcome.InvalidClass()
                    : new MarkAttendanceOutcome.Marked();
            });

        await _sut.MarkUniqueAttendance(request);
        return capturedAttendance;
    }
}

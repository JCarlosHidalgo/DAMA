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

    private Mock<IUniqueClassAttendanceDao> uniqueClassAttendanceDao = null!;
    private Mock<ICourseManagementClient> courseManagementClient = null!;
    private Mock<IAttendanceMarker> attendanceMarker = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IAttendanceClassBuilder> attendanceClassBuilder = null!;
    private Mock<IMapper> mapper = null!;

    private UniqueClassService sut = null!;

    [SetUp]
    public void SetUp()
    {
        uniqueClassAttendanceDao = new Mock<IUniqueClassAttendanceDao>(MockBehavior.Strict);
        courseManagementClient = new Mock<ICourseManagementClient>(MockBehavior.Strict);
        attendanceMarker = new Mock<IAttendanceMarker>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        attendanceClassBuilder = new Mock<IAttendanceClassBuilder>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);

        claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);

        sut = new UniqueClassService(
            uniqueClassAttendanceDao.Object,
            courseManagementClient.Object,
            attendanceMarker.Object,
            claimContext.Object,
            Options.Create(new AttendanceOptions { PageSize = 10 }),
            attendanceClassBuilder.Object,
            mapper.Object);
    }

    [Test]
    public async Task GetUniqueAttendance_ReturnsMappedResponses()
    {
        var classId = Guid.NewGuid();
        List<UniqueClassAttendance> attendances = [new UniqueClassAttendance()];
        List<UniqueAttendanceResponse> mapped = [new UniqueAttendanceResponse()];

        uniqueClassAttendanceDao
            .Setup(target => target.GetUniqueAttendanceAsync(CallerTenantId, classId))
            .ReturnsAsync(attendances);
        mapper.Setup(target => target.Map<List<UniqueAttendanceResponse>>(attendances)).Returns(mapped);

        List<UniqueAttendanceResponse> result = await sut.GetUniqueAttendance(classId);

        Assert.That(result, Is.SameAs(mapped));
    }

    [Test]
    public async Task GetUniqueAttendanceByStudentId_WhenStudentAccessingOther_ReturnsForbidden()
    {
        var studentId = Guid.NewGuid();
        claimContext.Setup(target => target.Role).Returns(UserRoles.Student);
        claimContext.Setup(target => target.UserId).Returns(CallerStudentId);

        GetUniqueByStudentOutcome result = await sut.GetUniqueAttendanceByStudentId(studentId);

        Assert.That(result, Is.InstanceOf<GetUniqueByStudentOutcome.Forbidden>());
    }

    [Test]
    public async Task GetUniqueAttendanceByStudentId_WhenAllowed_ReturnsFoundWithAttendances()
    {
        var studentId = Guid.NewGuid();
        List<UniqueClassAttendance> attendances = [new UniqueClassAttendance()];
        List<UniqueAttendanceResponse> mapped = [new UniqueAttendanceResponse()];

        claimContext.Setup(target => target.Role).Returns(UserRoles.Client);
        uniqueClassAttendanceDao
            .Setup(target => target.GetUniqueAttendanceByStudentIdAsync(CallerTenantId, studentId))
            .ReturnsAsync(attendances);
        mapper.Setup(target => target.Map<List<UniqueAttendanceResponse>>(attendances)).Returns(mapped);

        GetUniqueByStudentOutcome result = await sut.GetUniqueAttendanceByStudentId(studentId);

        Assert.That(result, Is.InstanceOf<GetUniqueByStudentOutcome.Found>());
    }

    [Test]
    public async Task ListMyUniqueAttendanceAsync_BuildsPage()
    {
        List<UniqueClassAttendance> attendances = [new UniqueClassAttendance()];
        List<UniqueAttendanceResponse> mapped = [new UniqueAttendanceResponse()];
        PageDto<UniqueAttendanceResponse> expectedPage = new() { CurrentIndex = 0, MaxIndex = 0, Items = mapped };

        claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        uniqueClassAttendanceDao
            .Setup(target => target.CountByStudentForTenantAsync(CallerTenantId, CallerStudentId))
            .ReturnsAsync(1);
        uniqueClassAttendanceDao
            .Setup(target => target.GetPageByStudentForTenantAsync(CallerTenantId, CallerStudentId, 0, 10))
            .ReturnsAsync(attendances);
        mapper.Setup(target => target.Map<List<UniqueAttendanceResponse>>(attendances)).Returns(mapped);
        attendanceClassBuilder.Setup(target => target.BuildPage(0, 0, mapped)).Returns(expectedPage);

        PageDto<UniqueAttendanceResponse> result = await sut.ListMyUniqueAttendanceAsync(0);

        Assert.That(result, Is.SameAs(expectedPage));
    }

    [Test]
    public async Task MarkUniqueAttendance_DelegatesToAttendanceMarker()
    {
        UniqueAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };

        attendanceMarker
            .Setup(target => target.MarkAsync<UniqueClassAttendance, UniqueAttendanceResponse>(
                It.IsAny<Func<AttendanceMarkContext, Task<AttendanceBuildResult<UniqueClassAttendance>?>>>(),
                It.IsAny<Func<UniqueClassAttendance, ITransactionContext, Task<int>>>(),
                It.IsAny<Func<UniqueClassAttendance, ITransactionContext, Task<bool>>>(),
                It.IsAny<Func<UniqueClassAttendance, string>>()))
            .ReturnsAsync(new MarkAttendanceOutcome.Marked());

        MarkAttendanceOutcome result = await sut.MarkUniqueAttendance(request);

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

        courseManagementClient
            .Setup(target => target.FindUniqueClassAsync(request.ClassId))
            .ReturnsAsync(classMetadata);
        attendanceClassBuilder
            .Setup(target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", metadataDate, request, classMetadata))
            .Returns(builtAttendance);

        UniqueClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.SameAs(builtAttendance));
        attendanceClassBuilder.Verify(
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

        courseManagementClient
            .Setup(target => target.FindUniqueClassAsync(request.ClassId))
            .ReturnsAsync(classMetadata);
        attendanceClassBuilder
            .Setup(target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", utcToday, request, classMetadata))
            .Returns(builtAttendance);

        UniqueClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.SameAs(builtAttendance));
        attendanceClassBuilder.Verify(
            target => target.BuildUniqueAttendance(
                CallerTenantId, CallerStudentId, "Pedro", utcToday, request, classMetadata),
            Times.Once);
    }

    [Test]
    public async Task MarkUniqueAttendance_WhenClassNotFound_ReturnsNullWithoutCallingBuilder()
    {
        UniqueAttendanceDto request = new() { ClassId = Guid.NewGuid(), CourseName = "Course" };
        AttendanceMarkContext markContext = new(CallerTenantId, CallerStudentId, "Pedro", "America/La_Paz");

        courseManagementClient
            .Setup(target => target.FindUniqueClassAsync(request.ClassId))
            .ReturnsAsync((ClassExistenceMeta?)null);

        UniqueClassAttendance? capturedAttendance = await InvokeMarkLambdaAsync(request, markContext);

        Assert.That(capturedAttendance, Is.Null);
        attendanceClassBuilder.VerifyNoOtherCalls();
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

        claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        uniqueClassAttendanceDao
            .Setup(target => target.CountByStudentForTenantAsync(CallerTenantId, CallerStudentId))
            .ReturnsAsync(1);
        mapper
            .Setup(target => target.Map<List<UniqueAttendanceResponse>>(
                It.Is<List<UniqueClassAttendance>>(list => list.Count == 0)))
            .Returns(emptyMapped);
        attendanceClassBuilder
            .Setup(target => target.BuildPage(pageIndexBeyondMax, 0, emptyMapped))
            .Returns(expectedPage);

        PageDto<UniqueAttendanceResponse> result = await sut.ListMyUniqueAttendanceAsync(pageIndexBeyondMax);

        Assert.That(result, Is.SameAs(expectedPage));
        uniqueClassAttendanceDao.Verify(
            target => target.GetPageByStudentForTenantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    private async Task<UniqueClassAttendance?> InvokeMarkLambdaAsync(
        UniqueAttendanceDto request,
        AttendanceMarkContext markContext)
    {
        UniqueClassAttendance? capturedAttendance = null;
        attendanceMarker
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
                    uniqueClassAttendanceDao
                        .Setup(target => target.TryMarkAttendanceAsync(capturedAttendance, It.IsAny<ITransactionContext>()))
                        .ReturnsAsync(true);
                    await tryMark(capturedAttendance, Mock.Of<ITransactionContext>());
                    resolveGroup(capturedAttendance);
                }
                return capturedAttendance is null
                    ? (MarkAttendanceOutcome)new MarkAttendanceOutcome.InvalidClass()
                    : new MarkAttendanceOutcome.Marked();
            });

        await sut.MarkUniqueAttendance(request);
        return capturedAttendance;
    }
}

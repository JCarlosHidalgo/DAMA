using AutoMapper;

using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Hubs;
using Backend.Options;
using Backend.Results.Attendance;
using Backend.Services.Concrete.Attendance;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Test.Services.Concrete.Attendance;

[TestFixture]
public class AttendanceMarkerTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CallerStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string CallerStudentName = "test student";
    private const string ValidTimezoneId = "America/La_Paz";

    private Mock<IClaimContext> claimContext = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IStudentRemainClassesDao> remainClassesDao = null!;
    private Mock<IHubContext<AttendanceHub>> hubContext = null!;
    private Mock<IHubClients> hubClients = null!;
    private Mock<IClientProxy> clientProxy = null!;
    private Mock<IMapper> mapper = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private AttendanceOptions options = null!;
    private AttendanceMarker sut = null!;

    [SetUp]
    public void SetUp()
    {
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        hubContext = new Mock<IHubContext<AttendanceHub>>(MockBehavior.Strict);
        hubClients = new Mock<IHubClients>(MockBehavior.Strict);
        clientProxy = new Mock<IClientProxy>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        options = new AttendanceOptions
        {
            AllowedWindowStart = new TimeOnly(0, 0),
            AllowedWindowEnd = new TimeOnly(23, 59, 59, 999)
        };

        sut = new AttendanceMarker(
            claimContext.Object,
            unitOfWork.Object,
            remainClassesDao.Object,
            hubContext.Object,
            mapper.Object,
            Options.Create(options),
            NullLogger<AttendanceMarker>.Instance);
    }

    [Test]
    public async Task MarkAsync_WhenTimezoneInvalid_ReturnsInvalidTenantTimezone()
    {
        claimContext.Setup(target => target.TenantTimezone).Returns("Continent/NonExistent");

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("x", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.InvalidTenantTimezone>());
        unitOfWork.Verify(target => target.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenNowIsOutsideWindow_ReturnsOutsideAllowedWindow()
    {
        options.AllowedWindowStart = new TimeOnly(0, 0);
        options.AllowedWindowEnd = new TimeOnly(0, 0);

        AttendanceMarker localSut = new(
            claimContext.Object, unitOfWork.Object, remainClassesDao.Object,
            hubContext.Object, mapper.Object, Options.Create(options),
            NullLogger<AttendanceMarker>.Instance);

        claimContext.Setup(target => target.TenantTimezone).Returns(ValidTimezoneId);

        MarkAttendanceOutcome result = await localSut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("x", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.OutsideAllowedWindow>());
        unitOfWork.Verify(target => target.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenResolveReturnsNull_ReturnsInvalidClass()
    {
        SetupClaimsForHappyPath();

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(null),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.InvalidClass>());
        unitOfWork.Verify(target => target.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenRemainDecrementFails_ReturnsNoRemainingClassesAndDoesNotCommit()
    {
        SetupClaimsForHappyPath();
        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, transactionScope.Object))
            .ReturnsAsync(false);

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.NoRemainingClasses>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenMarkAttendanceFails_ReturnsAlreadyMarkedAndDoesNotCommit()
    {
        SetupClaimsForHappyPath();
        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, transactionScope.Object))
            .ReturnsAsync(true);

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(false),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.AlreadyMarked>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenAllSucceed_CommitsAndBroadcastsAttendanceMarkedToGroup()
    {
        SetupClaimsForHappyPath();
        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, transactionScope.Object))
            .ReturnsAsync(true);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        string expectedResponse = "mapped_response";
        const string broadcastGroupName = "scheduled:test-group";
        mapper.Setup(target => target.Map<string>("attendance")).Returns(expectedResponse);

        hubContext.SetupGet(hub => hub.Clients).Returns(hubClients.Object);
        hubClients.Setup(clients => clients.Group(broadcastGroupName)).Returns(clientProxy.Object);
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync(
                "AttendanceMarked",
                It.Is<object?[]>(args => args.Length == 1 && (string)args[0]! == expectedResponse),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => broadcastGroupName);

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.Marked>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        clientProxy.Verify(proxy => proxy.SendCoreAsync(
                "AttendanceMarked",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task MarkAsync_WhenClassIsFull_ReturnsClassFullAndDoesNotDecrementOrCommit()
    {
        SetupClaimsForHappyPath();
        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 1)),
            (_, _) => Task.FromResult(1),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.ClassFull>());
        remainClassesDao.Verify(
            target => target.TryDecrementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenUnderLimit_ConsultsCountAndProceedsToMarked()
    {
        SetupClaimsForHappyPath();
        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, transactionScope.Object))
            .ReturnsAsync(true);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        const string broadcastGroupName = "scheduled:test-group";
        mapper.Setup(target => target.Map<string>("attendance")).Returns("mapped_response");
        hubContext.SetupGet(hub => hub.Clients).Returns(hubClients.Object);
        hubClients.Setup(clients => clients.Group(broadcastGroupName)).Returns(clientProxy.Object);
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync("AttendanceMarked", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarkAttendanceOutcome result = await sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 5)),
            (_, _) => Task.FromResult(2),
            (_, _) => Task.FromResult(true),
            _ => broadcastGroupName);

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.Marked>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    private void SetupClaimsForHappyPath()
    {
        claimContext.Setup(target => target.TenantTimezone).Returns(ValidTimezoneId);
        claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);
        claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        claimContext.Setup(target => target.UserName).Returns(CallerStudentName);
    }
}

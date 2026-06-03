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

    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IStudentRemainClassesDao> _remainClassesDao = null!;
    private Mock<IHubContext<AttendanceHub>> _hubContext = null!;
    private Mock<IHubClients> _hubClients = null!;
    private Mock<IClientProxy> _clientProxy = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private AttendanceOptions _options = null!;
    private AttendanceMarker _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        _hubContext = new Mock<IHubContext<AttendanceHub>>(MockBehavior.Strict);
        _hubClients = new Mock<IHubClients>(MockBehavior.Strict);
        _clientProxy = new Mock<IClientProxy>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _options = new AttendanceOptions
        {
            AllowedWindowStart = new TimeOnly(0, 0),
            AllowedWindowEnd = new TimeOnly(23, 59, 59, 999)
        };

        _sut = new AttendanceMarker(
            _claimContext.Object,
            _unitOfWork.Object,
            _remainClassesDao.Object,
            _hubContext.Object,
            _mapper.Object,
            Options.Create(_options),
            NullLogger<AttendanceMarker>.Instance);
    }

    [Test]
    public async Task MarkAsync_WhenTimezoneInvalid_ReturnsInvalidTenantTimezone()
    {
        _claimContext.Setup(target => target.TenantTimezone).Returns("Continent/NonExistent");

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("x", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.InvalidTenantTimezone>());
        _unitOfWork.Verify(target => target.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenNowIsOutsideWindow_ReturnsOutsideAllowedWindow()
    {
        _options.AllowedWindowStart = new TimeOnly(0, 0);
        _options.AllowedWindowEnd = new TimeOnly(0, 0);

        AttendanceMarker localSut = new(
            _claimContext.Object, _unitOfWork.Object, _remainClassesDao.Object,
            _hubContext.Object, _mapper.Object, Options.Create(_options),
            NullLogger<AttendanceMarker>.Instance);

        _claimContext.Setup(target => target.TenantTimezone).Returns(ValidTimezoneId);

        MarkAttendanceOutcome result = await localSut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("x", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.OutsideAllowedWindow>());
        _unitOfWork.Verify(target => target.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenResolveReturnsNull_ReturnsInvalidClass()
    {
        SetupClaimsForHappyPath();

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(null),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.InvalidClass>());
        _unitOfWork.Verify(target => target.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenRemainDecrementFails_ReturnsNoRemainingClassesAndDoesNotCommit()
    {
        SetupClaimsForHappyPath();
        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, _transactionScope.Object))
            .ReturnsAsync(false);

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.NoRemainingClasses>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenMarkAttendanceFails_ReturnsAlreadyMarkedAndDoesNotCommit()
    {
        SetupClaimsForHappyPath();
        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, _transactionScope.Object))
            .ReturnsAsync(true);

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(false),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.AlreadyMarked>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenAllSucceed_CommitsAndBroadcastsAttendanceMarkedToGroup()
    {
        SetupClaimsForHappyPath();
        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, _transactionScope.Object))
            .ReturnsAsync(true);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        string expectedResponse = "mapped_response";
        const string broadcastGroupName = "scheduled:test-group";
        _mapper.Setup(target => target.Map<string>("attendance")).Returns(expectedResponse);

        _hubContext.SetupGet(hub => hub.Clients).Returns(_hubClients.Object);
        _hubClients.Setup(clients => clients.Group(broadcastGroupName)).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(proxy => proxy.SendCoreAsync(
                "AttendanceMarked",
                It.Is<object?[]>(args => args.Length == 1 && (string)args[0]! == expectedResponse),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 0)),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(true),
            _ => broadcastGroupName);

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.Marked>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _clientProxy.Verify(proxy => proxy.SendCoreAsync(
                "AttendanceMarked",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task MarkAsync_WhenClassIsFull_ReturnsClassFullAndDoesNotDecrementOrCommit()
    {
        SetupClaimsForHappyPath();
        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 1)),
            (_, _) => Task.FromResult(1),
            (_, _) => Task.FromResult(true),
            _ => "group");

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.ClassFull>());
        _remainClassesDao.Verify(
            target => target.TryDecrementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task MarkAsync_WhenUnderLimit_ConsultsCountAndProceedsToMarked()
    {
        SetupClaimsForHappyPath();
        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _remainClassesDao
            .Setup(target => target.TryDecrementAsync(CallerTenantId, CallerStudentId, _transactionScope.Object))
            .ReturnsAsync(true);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        const string broadcastGroupName = "scheduled:test-group";
        _mapper.Setup(target => target.Map<string>("attendance")).Returns("mapped_response");
        _hubContext.SetupGet(hub => hub.Clients).Returns(_hubClients.Object);
        _hubClients.Setup(clients => clients.Group(broadcastGroupName)).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(proxy => proxy.SendCoreAsync("AttendanceMarked", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarkAttendanceOutcome result = await _sut.MarkAsync<string, string>(
            _ => Task.FromResult<AttendanceBuildResult<string>?>(new AttendanceBuildResult<string>("attendance", 5)),
            (_, _) => Task.FromResult(2),
            (_, _) => Task.FromResult(true),
            _ => broadcastGroupName);

        Assert.That(result, Is.InstanceOf<MarkAttendanceOutcome.Marked>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    private void SetupClaimsForHappyPath()
    {
        _claimContext.Setup(target => target.TenantTimezone).Returns(ValidTimezoneId);
        _claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);
        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _claimContext.Setup(target => target.UserName).Returns(CallerStudentName);
    }
}

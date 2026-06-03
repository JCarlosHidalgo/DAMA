using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.DB.Daos.Abstract.Single.Events;
using Backend.Events;
using Backend.Results.Events;
using Backend.Services.Concrete.Events;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Events;

[TestFixture]
public class ClassDeletedHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IProcessedEventDao> _processedEventDao = null!;
    private Mock<IScheduledClassAttendanceDao> _scheduledClassAttendanceDao = null!;
    private Mock<IUniqueClassAttendanceDao> _uniqueClassAttendanceDao = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private ClassDeletedHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        _scheduledClassAttendanceDao = new Mock<IScheduledClassAttendanceDao>(MockBehavior.Strict);
        _uniqueClassAttendanceDao = new Mock<IUniqueClassAttendanceDao>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _sut = new ClassDeletedHandler(
            _unitOfWork.Object,
            _processedEventDao.Object,
            _scheduledClassAttendanceDao.Object,
            _uniqueClassAttendanceDao.Object,
            NullLogger<ClassDeletedHandler>.Instance);
    }

    private static ClassDeletedEvent BuildEvent() => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "ClassDeleted",
        OccurredAt = DateTime.UtcNow,
        Data = new ClassDeletedData { ClassId = Guid.NewGuid(), TenantId = Guid.NewGuid() }
    };

    [Test]
    public async Task HandleAsync_FirstTime_DeletesScheduledAndUniqueAttendancesAndReturnsAttendancesDeleted()
    {
        ClassDeletedEvent classEvent = BuildEvent();

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(classEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(true);
        _scheduledClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(classEvent.Data.TenantId, classEvent.Data.ClassId, _transactionScope.Object))
            .ReturnsAsync(2);
        _uniqueClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(classEvent.Data.TenantId, classEvent.Data.ClassId, _transactionScope.Object))
            .ReturnsAsync(1);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        ClassDeletedOutcome result = await _sut.HandleAsync(classEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ClassDeletedOutcome.AttendancesDeleted>());
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessed()
    {
        ClassDeletedEvent classEvent = BuildEvent();

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(classEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(false);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        ClassDeletedOutcome result = await _sut.HandleAsync(classEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ClassDeletedOutcome.AlreadyProcessed>());
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        ClassDeletedEvent classEvent = BuildEvent();
        _unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        ClassDeletedOutcome result = await _sut.HandleAsync(classEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ClassDeletedOutcome.Failed>());
    }
}

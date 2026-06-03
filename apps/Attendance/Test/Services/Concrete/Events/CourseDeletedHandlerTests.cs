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
public class CourseDeletedHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IProcessedEventDao> _processedEventDao = null!;
    private Mock<IScheduledClassAttendanceDao> _scheduledClassAttendanceDao = null!;
    private Mock<IUniqueClassAttendanceDao> _uniqueClassAttendanceDao = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private CourseDeletedHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        _scheduledClassAttendanceDao = new Mock<IScheduledClassAttendanceDao>(MockBehavior.Strict);
        _uniqueClassAttendanceDao = new Mock<IUniqueClassAttendanceDao>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _sut = new CourseDeletedHandler(
            _unitOfWork.Object,
            _processedEventDao.Object,
            _scheduledClassAttendanceDao.Object,
            _uniqueClassAttendanceDao.Object,
            NullLogger<CourseDeletedHandler>.Instance);
    }

    private static CourseDeletedEvent BuildEvent(params Guid[] classIds) => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "CourseDeleted",
        OccurredAt = DateTime.UtcNow,
        Data = new CourseDeletedData
        {
            CourseId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClassIds = [.. classIds]
        }
    };

    [Test]
    public async Task HandleAsync_FirstTime_DeletesAllAttendancesForEachClassAndReturnsAttendancesDeleted()
    {
        var firstClassId = Guid.NewGuid();
        var secondClassId = Guid.NewGuid();
        CourseDeletedEvent courseEvent = BuildEvent(firstClassId, secondClassId);

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(courseEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(true);
        _scheduledClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, It.IsAny<Guid>(), _transactionScope.Object))
            .ReturnsAsync(0);
        _uniqueClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, It.IsAny<Guid>(), _transactionScope.Object))
            .ReturnsAsync(0);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        CourseDeletedOutcome result = await _sut.HandleAsync(courseEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CourseDeletedOutcome.AttendancesDeleted>());
        _scheduledClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, firstClassId, _transactionScope.Object), Times.Once);
        _scheduledClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, secondClassId, _transactionScope.Object), Times.Once);
        _uniqueClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, firstClassId, _transactionScope.Object), Times.Once);
        _uniqueClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, secondClassId, _transactionScope.Object), Times.Once);
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessed()
    {
        CourseDeletedEvent courseEvent = BuildEvent();

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(courseEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(false);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        CourseDeletedOutcome result = await _sut.HandleAsync(courseEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CourseDeletedOutcome.AlreadyProcessed>());
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        CourseDeletedEvent courseEvent = BuildEvent();
        _unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        CourseDeletedOutcome result = await _sut.HandleAsync(courseEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CourseDeletedOutcome.Failed>());
    }
}

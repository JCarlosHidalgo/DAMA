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
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IProcessedEventDao> processedEventDao = null!;
    private Mock<IScheduledClassAttendanceDao> scheduledClassAttendanceDao = null!;
    private Mock<IUniqueClassAttendanceDao> uniqueClassAttendanceDao = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private CourseDeletedHandler sut = null!;

    [SetUp]
    public void SetUp()
    {
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        scheduledClassAttendanceDao = new Mock<IScheduledClassAttendanceDao>(MockBehavior.Strict);
        uniqueClassAttendanceDao = new Mock<IUniqueClassAttendanceDao>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        sut = new CourseDeletedHandler(
            unitOfWork.Object,
            processedEventDao.Object,
            scheduledClassAttendanceDao.Object,
            uniqueClassAttendanceDao.Object,
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

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(courseEvent.EventId, transactionScope.Object))
            .ReturnsAsync(true);
        scheduledClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, It.IsAny<Guid>(), transactionScope.Object))
            .ReturnsAsync(0);
        uniqueClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, It.IsAny<Guid>(), transactionScope.Object))
            .ReturnsAsync(0);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        CourseDeletedOutcome result = await sut.HandleAsync(courseEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CourseDeletedOutcome.AttendancesDeleted>());
        scheduledClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, firstClassId, transactionScope.Object), Times.Once);
        scheduledClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, secondClassId, transactionScope.Object), Times.Once);
        uniqueClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, firstClassId, transactionScope.Object), Times.Once);
        uniqueClassAttendanceDao.Verify(target => target.DeleteByClassForTenantAsync(courseEvent.Data.TenantId, secondClassId, transactionScope.Object), Times.Once);
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessed()
    {
        CourseDeletedEvent courseEvent = BuildEvent();

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(courseEvent.EventId, transactionScope.Object))
            .ReturnsAsync(false);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        CourseDeletedOutcome result = await sut.HandleAsync(courseEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CourseDeletedOutcome.AlreadyProcessed>());
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        CourseDeletedEvent courseEvent = BuildEvent();
        unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        CourseDeletedOutcome result = await sut.HandleAsync(courseEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CourseDeletedOutcome.Failed>());
    }
}

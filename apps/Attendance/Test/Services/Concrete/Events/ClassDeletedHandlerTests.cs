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
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IProcessedEventDao> processedEventDao = null!;
    private Mock<IScheduledClassAttendanceDao> scheduledClassAttendanceDao = null!;
    private Mock<IUniqueClassAttendanceDao> uniqueClassAttendanceDao = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private ClassDeletedHandler sut = null!;

    [SetUp]
    public void SetUp()
    {
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        scheduledClassAttendanceDao = new Mock<IScheduledClassAttendanceDao>(MockBehavior.Strict);
        uniqueClassAttendanceDao = new Mock<IUniqueClassAttendanceDao>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        sut = new ClassDeletedHandler(
            unitOfWork.Object,
            processedEventDao.Object,
            scheduledClassAttendanceDao.Object,
            uniqueClassAttendanceDao.Object,
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

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(classEvent.EventId, transactionScope.Object))
            .ReturnsAsync(true);
        scheduledClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(classEvent.Data.TenantId, classEvent.Data.ClassId, transactionScope.Object))
            .ReturnsAsync(2);
        uniqueClassAttendanceDao
            .Setup(target => target.DeleteByClassForTenantAsync(classEvent.Data.TenantId, classEvent.Data.ClassId, transactionScope.Object))
            .ReturnsAsync(1);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        ClassDeletedOutcome result = await sut.HandleAsync(classEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ClassDeletedOutcome.AttendancesDeleted>());
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessed()
    {
        ClassDeletedEvent classEvent = BuildEvent();

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(classEvent.EventId, transactionScope.Object))
            .ReturnsAsync(false);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        ClassDeletedOutcome result = await sut.HandleAsync(classEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ClassDeletedOutcome.AlreadyProcessed>());
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        ClassDeletedEvent classEvent = BuildEvent();
        unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        ClassDeletedOutcome result = await sut.HandleAsync(classEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ClassDeletedOutcome.Failed>());
    }
}

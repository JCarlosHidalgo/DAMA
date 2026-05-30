using Backend.DB.Daos.Abstract.Single.Events;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Events;
using Backend.Results.Events;
using Backend.Services.Concrete.Events;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Events;

[TestFixture]
public class StudentRegisteredHandlerTests
{
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IProcessedEventDao> processedEventDao = null!;
    private Mock<IStudentRemainClassesDao> remainClassesDao = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private StudentRegisteredHandler sut = null!;

    [SetUp]
    public void SetUp()
    {
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        sut = new StudentRegisteredHandler(
            unitOfWork.Object,
            processedEventDao.Object,
            remainClassesDao.Object,
            NullLogger<StudentRegisteredHandler>.Instance);
    }

    private static StudentRegisteredEvent BuildEvent() => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "StudentRegistered",
        OccurredAt = DateTime.UtcNow,
        Data = new StudentRegisteredData
        {
            StudentId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserName = "new_student",
            RegisteredAt = DateTime.UtcNow
        }
    };

    [Test]
    public async Task HandleAsync_FirstTime_CreatesRemainAndReturnsRemainCreated()
    {
        StudentRegisteredEvent studentRegisteredEvent = BuildEvent();

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(studentRegisteredEvent.EventId, transactionScope.Object))
            .ReturnsAsync(true);
        remainClassesDao
            .Setup(target => target.IncrementAsync(
                studentRegisteredEvent.Data.TenantId,
                studentRegisteredEvent.Data.StudentId,
                0,
                studentRegisteredEvent.Data.UserName,
                transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        StudentRegisteredOutcome result = await sut.HandleAsync(studentRegisteredEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<StudentRegisteredOutcome.RemainCreated>());
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessedAndCommits()
    {
        StudentRegisteredEvent studentRegisteredEvent = BuildEvent();

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(studentRegisteredEvent.EventId, transactionScope.Object))
            .ReturnsAsync(false);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        StudentRegisteredOutcome result = await sut.HandleAsync(studentRegisteredEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<StudentRegisteredOutcome.AlreadyProcessed>());
        remainClassesDao.Verify(
            target => target.IncrementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ITransactionContext>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        StudentRegisteredEvent studentRegisteredEvent = BuildEvent();
        unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        StudentRegisteredOutcome result = await sut.HandleAsync(studentRegisteredEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<StudentRegisteredOutcome.Failed>());
    }
}

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
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IProcessedEventDao> _processedEventDao = null!;
    private Mock<IStudentRemainClassesDao> _remainClassesDao = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private StudentRegisteredHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        _remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _sut = new StudentRegisteredHandler(
            _unitOfWork.Object,
            _processedEventDao.Object,
            _remainClassesDao.Object,
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

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(studentRegisteredEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(true);
        _remainClassesDao
            .Setup(target => target.IncrementAsync(
                studentRegisteredEvent.Data.TenantId,
                studentRegisteredEvent.Data.StudentId,
                0,
                studentRegisteredEvent.Data.UserName,
                _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        StudentRegisteredOutcome result = await _sut.HandleAsync(studentRegisteredEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<StudentRegisteredOutcome.RemainCreated>());
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessedAndCommits()
    {
        StudentRegisteredEvent studentRegisteredEvent = BuildEvent();

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(studentRegisteredEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(false);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        StudentRegisteredOutcome result = await _sut.HandleAsync(studentRegisteredEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<StudentRegisteredOutcome.AlreadyProcessed>());
        _remainClassesDao.Verify(
            target => target.IncrementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ITransactionContext>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        StudentRegisteredEvent studentRegisteredEvent = BuildEvent();
        _unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        StudentRegisteredOutcome result = await _sut.HandleAsync(studentRegisteredEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<StudentRegisteredOutcome.Failed>());
    }
}

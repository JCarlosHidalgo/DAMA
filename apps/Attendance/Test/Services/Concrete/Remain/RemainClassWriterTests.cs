using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Results.Remain;
using Backend.Services.Concrete.Remain;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Services.Concrete.Remain;

[TestFixture]
public class RemainClassWriterTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Mock<IStudentRemainClassesDao> _remainClassesDao = null!;
    private Mock<IRemainRequestDao> _remainRequestDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private RemainClassWriter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        _remainRequestDao = new Mock<IRemainRequestDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);

        _sut = new RemainClassWriter(
            _remainClassesDao.Object,
            _remainRequestDao.Object,
            _unitOfWork.Object,
            _claimContext.Object);
    }

    [Test]
    public async Task IncrementForStudentByClientAsync_FirstRequest_IncrementsInsideTransactionAndCommits()
    {
        var requestId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, _transactionScope.Object))
            .ReturnsAsync(true);
        _remainClassesDao
            .Setup(target => target.IncrementAsync(CallerTenantId, studentId, 5, "Student", _transactionScope.Object))
            .Returns(Task.CompletedTask);

        IncrementStudentRemainOutcome outcome =
            await _sut.IncrementForStudentByClientAsync(requestId, studentId, 5, "Student");

        Assert.That(outcome, Is.InstanceOf<IncrementStudentRemainOutcome.Applied>());
        _remainClassesDao.Verify(
            target => target.IncrementAsync(CallerTenantId, studentId, 5, "Student", _transactionScope.Object),
            Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task IncrementForStudentByClientAsync_DuplicateRequest_SkipsIncrementAndCommits()
    {
        var requestId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, _transactionScope.Object))
            .ReturnsAsync(false);

        IncrementStudentRemainOutcome outcome =
            await _sut.IncrementForStudentByClientAsync(requestId, studentId, 5, "Student");

        Assert.That(outcome, Is.InstanceOf<IncrementStudentRemainOutcome.AlreadyApplied>());
        _remainClassesDao.Verify(
            target => target.IncrementAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task IncrementAllInTenantByClientAsync_FirstRequest_ReturnsAffectedRowCount()
    {
        var requestId = Guid.NewGuid();
        _remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, _transactionScope.Object))
            .ReturnsAsync(true);
        _remainClassesDao
            .Setup(target => target.IncrementAllInTenantAsync(CallerTenantId, 3, _transactionScope.Object))
            .ReturnsAsync(42);

        IncrementTenantRemainOutcome outcome = await _sut.IncrementAllInTenantByClientAsync(requestId, 3);

        Assert.That(outcome, Is.InstanceOf<IncrementTenantRemainOutcome.Applied>());
        Assert.That(((IncrementTenantRemainOutcome.Applied)outcome).Affected, Is.EqualTo(42));
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task IncrementAllInTenantByClientAsync_DuplicateRequest_SkipsIncrement()
    {
        var requestId = Guid.NewGuid();
        _remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, _transactionScope.Object))
            .ReturnsAsync(false);

        IncrementTenantRemainOutcome outcome = await _sut.IncrementAllInTenantByClientAsync(requestId, 3);

        Assert.That(outcome, Is.InstanceOf<IncrementTenantRemainOutcome.AlreadyApplied>());
        _remainClassesDao.Verify(
            target => target.IncrementAllInTenantAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

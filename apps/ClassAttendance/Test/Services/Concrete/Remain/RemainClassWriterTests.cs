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

    private Mock<IStudentRemainClassesDao> remainClassesDao = null!;
    private Mock<IRemainRequestDao> remainRequestDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private RemainClassWriter sut = null!;

    [SetUp]
    public void SetUp()
    {
        remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        remainRequestDao = new Mock<IRemainRequestDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);

        sut = new RemainClassWriter(
            remainClassesDao.Object,
            remainRequestDao.Object,
            unitOfWork.Object,
            claimContext.Object);
    }

    [Test]
    public async Task IncrementForStudentByClientAsync_FirstRequest_IncrementsInsideTransactionAndCommits()
    {
        var requestId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, transactionScope.Object))
            .ReturnsAsync(true);
        remainClassesDao
            .Setup(target => target.IncrementAsync(CallerTenantId, studentId, 5, "Student", transactionScope.Object))
            .Returns(Task.CompletedTask);

        IncrementStudentRemainOutcome outcome =
            await sut.IncrementForStudentByClientAsync(requestId, studentId, 5, "Student");

        Assert.That(outcome, Is.InstanceOf<IncrementStudentRemainOutcome.Applied>());
        remainClassesDao.Verify(
            target => target.IncrementAsync(CallerTenantId, studentId, 5, "Student", transactionScope.Object),
            Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task IncrementForStudentByClientAsync_DuplicateRequest_SkipsIncrementAndCommits()
    {
        var requestId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, transactionScope.Object))
            .ReturnsAsync(false);

        IncrementStudentRemainOutcome outcome =
            await sut.IncrementForStudentByClientAsync(requestId, studentId, 5, "Student");

        Assert.That(outcome, Is.InstanceOf<IncrementStudentRemainOutcome.AlreadyApplied>());
        remainClassesDao.Verify(
            target => target.IncrementAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task IncrementAllInTenantByClientAsync_FirstRequest_ReturnsAffectedRowCount()
    {
        var requestId = Guid.NewGuid();
        remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, transactionScope.Object))
            .ReturnsAsync(true);
        remainClassesDao
            .Setup(target => target.IncrementAllInTenantAsync(CallerTenantId, 3, transactionScope.Object))
            .ReturnsAsync(42);

        IncrementTenantRemainOutcome outcome = await sut.IncrementAllInTenantByClientAsync(requestId, 3);

        Assert.That(outcome, Is.InstanceOf<IncrementTenantRemainOutcome.Applied>());
        Assert.That(((IncrementTenantRemainOutcome.Applied)outcome).Affected, Is.EqualTo(42));
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task IncrementAllInTenantByClientAsync_DuplicateRequest_SkipsIncrement()
    {
        var requestId = Guid.NewGuid();
        remainRequestDao
            .Setup(target => target.TryMarkProcessedAsync(requestId, transactionScope.Object))
            .ReturnsAsync(false);

        IncrementTenantRemainOutcome outcome = await sut.IncrementAllInTenantByClientAsync(requestId, 3);

        Assert.That(outcome, Is.InstanceOf<IncrementTenantRemainOutcome.AlreadyApplied>());
        remainClassesDao.Verify(
            target => target.IncrementAllInTenantAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

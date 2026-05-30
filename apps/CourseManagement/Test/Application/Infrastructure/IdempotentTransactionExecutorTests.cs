using Backend.Application.Infrastructure;
using Backend.DB.Daos.Abstract.Single;
using Backend.Entities;
using Backend.Entities.Courses;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Infrastructure;

[TestFixture]
public class IdempotentTransactionExecutorTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string EntityType = "Course";

    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<ICourseIdempotencyDao> idempotencyDao = null!;
    private IdempotentTransactionExecutor executor = null!;

    [SetUp]
    public void SetUp()
    {
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        idempotencyDao = new Mock<ICourseIdempotencyDao>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(transactionScope.Object);

        executor = new IdempotentTransactionExecutor(unitOfWork.Object, idempotencyDao.Object);
    }

    [Test]
    public async Task ExecuteAsync_WithNullExternalReferenceAndInsertSucceeds_ReturnsInsertedAndCommits()
    {
        var newEntityId = Guid.NewGuid();
        var candidate = new Course { Id = newEntityId, Name = "Curso", TenantId = TenantId };

        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: null,
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(candidate),
            loadPrior: _ => Task.FromResult<Course?>(null));

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.Inserted>());
            Assert.That(((IdempotentInsertOutcome<Course>.Inserted)outcome).Entity, Is.SameAs(candidate));
        });
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        idempotencyDao.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyExternalReferenceAndInsertSucceeds_SkipsLedgerAndCommits()
    {
        var newEntityId = Guid.NewGuid();
        var candidate = new Course { Id = newEntityId };

        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: string.Empty,
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(candidate),
            loadPrior: _ => Task.FromResult<Course?>(null));

        Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.Inserted>());
        idempotencyDao.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecuteAsync_WithNullExternalReferenceAndInsertReturnsNull_ReturnsInsertFailedAndDoesNotCommit()
    {
        var newEntityId = Guid.NewGuid();

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: null,
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(null),
            loadPrior: _ => Task.FromResult<Course?>(null));

        Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.InsertFailed>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WithExternalReferenceAndLedgerSucceedsAndInsertSucceeds_ReturnsInserted()
    {
        var newEntityId = Guid.NewGuid();
        var candidate = new Course { Id = newEntityId };

        idempotencyDao
            .Setup(dao => dao.TryRecordAsync(
                It.Is<CourseIdempotency>(record =>
                    record.TenantId == TenantId
                    && record.ExternalReference == "ref-001"
                    && record.EntityType == EntityType
                    && record.EntityId == newEntityId),
                transactionScope.Object))
            .ReturnsAsync(true);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: "ref-001",
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(candidate),
            loadPrior: _ => Task.FromResult<Course?>(null));

        Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.Inserted>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WithExternalReferenceAndLedgerSucceedsButInsertReturnsNull_ReturnsInsertFailed()
    {
        var newEntityId = Guid.NewGuid();

        idempotencyDao
            .Setup(dao => dao.TryRecordAsync(It.IsAny<CourseIdempotency>(), transactionScope.Object))
            .ReturnsAsync(true);

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: "ref-002",
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(null),
            loadPrior: _ => Task.FromResult<Course?>(null));

        Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.InsertFailed>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WithDuplicateLedgerAndPriorExists_ReturnsReplayedWithPrior()
    {
        var newEntityId = Guid.NewGuid();
        var priorEntityId = Guid.NewGuid();
        var prior = new Course { Id = priorEntityId, Name = "Prior" };

        idempotencyDao
            .Setup(dao => dao.TryRecordAsync(It.IsAny<CourseIdempotency>(), transactionScope.Object))
            .ReturnsAsync(false);
        idempotencyDao
            .Setup(dao => dao.GetByExternalReferenceAsync(TenantId, "ref-dup"))
            .ReturnsAsync(new CourseIdempotency
            {
                TenantId = TenantId,
                ExternalReference = "ref-dup",
                EntityType = EntityType,
                EntityId = priorEntityId
            });

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: "ref-dup",
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(new Course { Id = newEntityId }),
            loadPrior: id =>
            {
                Assert.That(id, Is.EqualTo(priorEntityId));
                return Task.FromResult<Course?>(prior);
            });

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.Replayed>());
            Assert.That(((IdempotentInsertOutcome<Course>.Replayed)outcome).Prior, Is.SameAs(prior));
        });
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WithDuplicateLedgerButLoadPriorReturnsNull_ReturnsInsertFailed()
    {
        var newEntityId = Guid.NewGuid();
        var priorEntityId = Guid.NewGuid();

        idempotencyDao
            .Setup(dao => dao.TryRecordAsync(It.IsAny<CourseIdempotency>(), transactionScope.Object))
            .ReturnsAsync(false);
        idempotencyDao
            .Setup(dao => dao.GetByExternalReferenceAsync(TenantId, "ref-dup-orphan"))
            .ReturnsAsync(new CourseIdempotency { EntityId = priorEntityId });

        IdempotentInsertOutcome<Course> outcome = await executor.ExecuteAsync<Course>(
            TenantId,
            externalReference: "ref-dup-orphan",
            EntityType,
            newEntityId,
            insert: _ => Task.FromResult<Course?>(new Course()),
            loadPrior: _ => Task.FromResult<Course?>(null));

        Assert.That(outcome, Is.InstanceOf<IdempotentInsertOutcome<Course>.InsertFailed>());
    }

    [Test]
    public void ExecuteAsync_WithDuplicateLedgerButIdempotencyRecordVanished_ThrowsInvalidOperationException()
    {
        var newEntityId = Guid.NewGuid();

        idempotencyDao
            .Setup(dao => dao.TryRecordAsync(It.IsAny<CourseIdempotency>(), transactionScope.Object))
            .ReturnsAsync(false);
        idempotencyDao
            .Setup(dao => dao.GetByExternalReferenceAsync(TenantId, "ref-vanished"))
            .ReturnsAsync((CourseIdempotency?)null);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await executor.ExecuteAsync<Course>(
                TenantId,
                externalReference: "ref-vanished",
                EntityType,
                newEntityId,
                insert: _ => Task.FromResult<Course?>(new Course()),
                loadPrior: _ => Task.FromResult<Course?>(null));
        });
    }
}

using Backend.Application.Infrastructure;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Infrastructure;

[TestFixture]
public class ClassCreationCoordinatorTests
{
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CourseId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IIdempotentTransactionExecutor> idempotentExecutor = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        idempotentExecutor = new Mock<IIdempotentTransactionExecutor>(MockBehavior.Strict);
    }

    [Test]
    public async Task CreateAsync_WhenCourseDoesNotExist_ReturnsCourseMissingAndSkipsExecutor()
    {
        var writer = new Mock<IClassAggregateWriter<ScheduledClass>>(MockBehavior.Strict);
        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(false);
        var coordinator =
            new ClassCreationCoordinator<ScheduledClass>(courseDao.Object, idempotentExecutor.Object, writer.Object);

        ClassCreationOutcome<ScheduledClass> outcome = await coordinator.CreateAsync(
            TenantId, CourseId, externalReference: null, "ScheduledClass",
            newEntityId: Guid.NewGuid(),
            entity: new ScheduledClass(),
            teachers: new List<ClassTeacher>());

        Assert.That(outcome, Is.InstanceOf<ClassCreationOutcome<ScheduledClass>.CourseMissing>());
        idempotentExecutor.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CreateAsync_WhenCourseExistsAndExecutorReturnsInserted_ReturnsCreated()
    {
        var candidate = new ScheduledClass { Id = Guid.NewGuid() };
        var writer = new Mock<IClassAggregateWriter<ScheduledClass>>(MockBehavior.Strict);

        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(true);
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<ScheduledClass>(
                TenantId, "ref-1", "ScheduledClass", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<ScheduledClass?>>>(),
                It.IsAny<Func<Guid, Task<ScheduledClass?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<ScheduledClass>.Inserted(candidate));

        var coordinator =
            new ClassCreationCoordinator<ScheduledClass>(courseDao.Object, idempotentExecutor.Object, writer.Object);

        ClassCreationOutcome<ScheduledClass> outcome = await coordinator.CreateAsync(
            TenantId, CourseId, "ref-1", "ScheduledClass", candidate.Id, candidate, new List<ClassTeacher>());

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.InstanceOf<ClassCreationOutcome<ScheduledClass>.Created>());
            Assert.That(((ClassCreationOutcome<ScheduledClass>.Created)outcome).Entity, Is.SameAs(candidate));
        });
    }

    [Test]
    public async Task CreateAsync_WhenExecutorReturnsReplayed_ReturnsReplayed()
    {
        var prior = new UniqueClass { Id = Guid.NewGuid() };
        var writer = new Mock<IClassAggregateWriter<UniqueClass>>(MockBehavior.Strict);

        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(true);
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<UniqueClass>(
                TenantId, "ref-dup", "UniqueClass", It.IsAny<Guid>(),
                It.IsAny<Func<ITransactionContext, Task<UniqueClass?>>>(),
                It.IsAny<Func<Guid, Task<UniqueClass?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<UniqueClass>.Replayed(prior));

        var coordinator =
            new ClassCreationCoordinator<UniqueClass>(courseDao.Object, idempotentExecutor.Object, writer.Object);

        ClassCreationOutcome<UniqueClass> outcome = await coordinator.CreateAsync(
            TenantId, CourseId, "ref-dup", "UniqueClass",
            newEntityId: Guid.NewGuid(),
            entity: new UniqueClass(),
            teachers: new List<ClassTeacher>());

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.InstanceOf<ClassCreationOutcome<UniqueClass>.Replayed>());
            Assert.That(((ClassCreationOutcome<UniqueClass>.Replayed)outcome).Prior, Is.SameAs(prior));
        });
    }

    [Test]
    public void CreateAsync_WhenExecutorReturnsInsertFailed_ThrowsUnreachableException()
    {
        var writer = new Mock<IClassAggregateWriter<ScheduledClass>>(MockBehavior.Strict);

        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(true);
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<ScheduledClass>(
                TenantId, null, "ScheduledClass", It.IsAny<Guid>(),
                It.IsAny<Func<ITransactionContext, Task<ScheduledClass?>>>(),
                It.IsAny<Func<Guid, Task<ScheduledClass?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<ScheduledClass>.InsertFailed());

        var coordinator =
            new ClassCreationCoordinator<ScheduledClass>(courseDao.Object, idempotentExecutor.Object, writer.Object);

        Assert.ThrowsAsync<System.Diagnostics.UnreachableException>(async () =>
        {
            await coordinator.CreateAsync(
                TenantId, CourseId, externalReference: null, "ScheduledClass",
                newEntityId: Guid.NewGuid(),
                entity: new ScheduledClass(),
                teachers: new List<ClassTeacher>());
        });
    }

    [Test]
    public async Task CreateAsync_PassesInsertDelegateThatInvokesWriterAndForwardsTeachers()
    {
        var entityId = Guid.NewGuid();
        var entity = new ScheduledClass { Id = entityId };
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = Guid.NewGuid(), TeacherName = "Profesor A" },
            new() { TeacherId = Guid.NewGuid(), TeacherName = "Profesor B" }
        };
        var writer = new Mock<IClassAggregateWriter<ScheduledClass>>(MockBehavior.Strict);
        var transactionContext = new Mock<ITransactionContext>(MockBehavior.Strict);
        writer.Setup(w => w.CreateForTenantAsync(entity, TenantId, transactionContext.Object)).ReturnsAsync(true);
        writer.Setup(w => w.InsertTeacherAsync(entityId, It.IsAny<ClassTeacher>(), TenantId, transactionContext.Object))
              .Returns(Task.CompletedTask);

        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(true);
        Func<ITransactionContext, Task<ScheduledClass?>>? capturedInsert = null;
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<ScheduledClass>(
                TenantId, null, "ScheduledClass", entityId,
                It.IsAny<Func<ITransactionContext, Task<ScheduledClass?>>>(),
                It.IsAny<Func<Guid, Task<ScheduledClass?>>>()))
            .Callback<Guid, string?, string, Guid, Func<ITransactionContext, Task<ScheduledClass?>>, Func<Guid, Task<ScheduledClass?>>>(
                (_, _, _, _, insertDelegate, _) => capturedInsert = insertDelegate)
            .ReturnsAsync(new IdempotentInsertOutcome<ScheduledClass>.Inserted(entity));

        var coordinator =
            new ClassCreationCoordinator<ScheduledClass>(courseDao.Object, idempotentExecutor.Object, writer.Object);

        await coordinator.CreateAsync(TenantId, CourseId, null, "ScheduledClass", entityId, entity, teachers);
        ScheduledClass? insertResult = await capturedInsert!(transactionContext.Object);

        Assert.That(insertResult, Is.SameAs(entity));
        writer.Verify(w => w.CreateForTenantAsync(entity, TenantId, transactionContext.Object), Times.Once);
        writer.Verify(w => w.InsertTeacherAsync(entityId, teachers[0], TenantId, transactionContext.Object), Times.Once);
        writer.Verify(w => w.InsertTeacherAsync(entityId, teachers[1], TenantId, transactionContext.Object), Times.Once);
    }

    [Test]
    public async Task CreateAsync_InsertDelegateReturnsNullWhenWriterCreateReturnsFalse()
    {
        var entityId = Guid.NewGuid();
        var entity = new ScheduledClass { Id = entityId };
        var writer = new Mock<IClassAggregateWriter<ScheduledClass>>(MockBehavior.Strict);
        var transactionContext = new Mock<ITransactionContext>(MockBehavior.Strict);
        writer.Setup(w => w.CreateForTenantAsync(entity, TenantId, transactionContext.Object)).ReturnsAsync(false);

        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(true);
        Func<ITransactionContext, Task<ScheduledClass?>>? capturedInsert = null;
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<ScheduledClass>(
                TenantId, null, "ScheduledClass", entityId,
                It.IsAny<Func<ITransactionContext, Task<ScheduledClass?>>>(),
                It.IsAny<Func<Guid, Task<ScheduledClass?>>>()))
            .Callback<Guid, string?, string, Guid, Func<ITransactionContext, Task<ScheduledClass?>>, Func<Guid, Task<ScheduledClass?>>>(
                (_, _, _, _, insertDelegate, _) => capturedInsert = insertDelegate)
            .ReturnsAsync(new IdempotentInsertOutcome<ScheduledClass>.Inserted(entity));

        var coordinator =
            new ClassCreationCoordinator<ScheduledClass>(courseDao.Object, idempotentExecutor.Object, writer.Object);

        await coordinator.CreateAsync(TenantId, CourseId, null, "ScheduledClass", entityId,
            entity, new List<ClassTeacher>());
        ScheduledClass? insertResult = await capturedInsert!(transactionContext.Object);

        Assert.That(insertResult, Is.Null);
        writer.Verify(w => w.InsertTeacherAsync(It.IsAny<Guid>(), It.IsAny<ClassTeacher>(), It.IsAny<Guid>(), It.IsAny<ITransactionContext>()), Times.Never);
    }
}

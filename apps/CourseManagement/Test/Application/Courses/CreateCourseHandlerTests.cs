using AutoMapper;

using Backend.Application.Courses;
using Backend.Application.Infrastructure;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Input;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Courses;

[TestFixture]
public class CreateCourseHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IIdempotentTransactionExecutor> idempotentExecutor = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<ICourseBuilder> courseBuilder = null!;
    private Mock<IMapper> mapper = null!;
    private CreateCourseHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        idempotentExecutor = new Mock<IIdempotentTransactionExecutor>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        courseBuilder = new Mock<ICourseBuilder>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new CreateCourseHandler(
            courseDao.Object,
            idempotentExecutor.Object,
            claimContext.Object,
            courseBuilder.Object,
            mapper.Object);
    }

    [Test]
    public async Task Handle_WhenExecutorReturnsInserted_ReturnsCreatedWithMappedDto()
    {
        var payload = new CreateCourseDto { Name = "Curso Demo", ExternalReference = "ref-1" };
        var candidate = new Course { Id = Guid.NewGuid(), Name = "Curso Demo", TenantId = TenantId };
        var mapped = new GetCourseDto { Id = candidate.Id, Name = candidate.Name };

        courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, "ref-1", "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.Inserted(candidate));
        mapper.Setup(map => map.Map<GetCourseDto>(candidate)).Returns(mapped);

        CreateCourseResult result = await handler.Handle(new CreateCourseCommand(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreateCourseResult.Created>());
            Assert.That(((CreateCourseResult.Created)result).Course, Is.SameAs(mapped));
        });
    }

    [Test]
    public async Task Handle_WhenExecutorReturnsReplayed_ReturnsReplayedFromIdempotencyWithMappedPrior()
    {
        var payload = new CreateCourseDto { Name = "Curso Demo", ExternalReference = "ref-dup" };
        var candidate = new Course { Id = Guid.NewGuid(), Name = "Curso Demo", TenantId = TenantId };
        var prior = new Course { Id = Guid.NewGuid(), Name = "Curso Previo", TenantId = TenantId };
        var mappedPrior = new GetCourseDto { Id = prior.Id, Name = prior.Name };

        courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, "ref-dup", "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.Replayed(prior));
        mapper.Setup(map => map.Map<GetCourseDto>(prior)).Returns(mappedPrior);

        CreateCourseResult result = await handler.Handle(new CreateCourseCommand(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreateCourseResult.ReplayedFromIdempotency>());
            Assert.That(((CreateCourseResult.ReplayedFromIdempotency)result).Course, Is.SameAs(mappedPrior));
        });
    }

    [Test]
    public void Handle_WhenExecutorReturnsInsertFailed_ThrowsUnreachableException()
    {
        var payload = new CreateCourseDto { Name = "Curso Demo", ExternalReference = null };
        var candidate = new Course { Id = Guid.NewGuid(), Name = "Curso Demo", TenantId = TenantId };

        courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, null, "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.InsertFailed());

        Assert.ThrowsAsync<System.Diagnostics.UnreachableException>(async () => await handler.Handle(new CreateCourseCommand(payload)));
    }

    [Test]
    public async Task Handle_PassesInsertDelegateThatInvokesCourseDaoCreate()
    {
        var payload = new CreateCourseDto { Name = "Curso Demo", ExternalReference = "ref-delegate" };
        var candidate = new Course { Id = Guid.NewGuid(), Name = "Curso Demo", TenantId = TenantId };
        var mapped = new GetCourseDto { Id = candidate.Id, Name = candidate.Name };
        var transactionContext = new Mock<ITransactionContext>(MockBehavior.Strict);

        courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        courseDao.Setup(dao => dao.CreateAsync(candidate, transactionContext.Object)).Returns(Task.CompletedTask);
        mapper.Setup(map => map.Map<GetCourseDto>(candidate)).Returns(mapped);

        Func<ITransactionContext, Task<Course?>>? capturedInsert = null;
        idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, "ref-delegate", "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .Callback<Guid, string?, string, Guid, Func<ITransactionContext, Task<Course?>>, Func<Guid, Task<Course?>>>(
                (_, _, _, _, insertDelegate, _) => capturedInsert = insertDelegate)
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.Inserted(candidate));

        await handler.Handle(new CreateCourseCommand(payload));
        Course? inserted = await capturedInsert!(transactionContext.Object);

        Assert.That(inserted, Is.SameAs(candidate));
        courseDao.Verify(dao => dao.CreateAsync(candidate, transactionContext.Object), Times.Once);
    }
}

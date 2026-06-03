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

    private Mock<ICourseDao> _courseDao = null!;
    private Mock<IIdempotentTransactionExecutor> _idempotentExecutor = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<ICourseBuilder> _courseBuilder = null!;
    private Mock<IMapper> _mapper = null!;
    private CreateCourseHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        _idempotentExecutor = new Mock<IIdempotentTransactionExecutor>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _courseBuilder = new Mock<ICourseBuilder>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new CreateCourseHandler(
            _courseDao.Object,
            _idempotentExecutor.Object,
            _claimContext.Object,
            _courseBuilder.Object,
            _mapper.Object);
    }

    [Test]
    public async Task Handle_WhenExecutorReturnsInserted_ReturnsCreatedWithMappedDto()
    {
        var payload = new CreateCourseDto { Name = "Curso Demo", ExternalReference = "ref-1" };
        var candidate = new Course { Id = Guid.NewGuid(), Name = "Curso Demo", TenantId = TenantId };
        var mapped = new GetCourseDto { Id = candidate.Id, Name = candidate.Name };

        _courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        _idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, "ref-1", "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.Inserted(candidate));
        _mapper.Setup(map => map.Map<GetCourseDto>(candidate)).Returns(mapped);

        CreateCourseResult result = await _handler.Handle(new CreateCourseCommand(payload));

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

        _courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        _idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, "ref-dup", "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.Replayed(prior));
        _mapper.Setup(map => map.Map<GetCourseDto>(prior)).Returns(mappedPrior);

        CreateCourseResult result = await _handler.Handle(new CreateCourseCommand(payload));

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

        _courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        _idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, null, "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.InsertFailed());

        Assert.ThrowsAsync<System.Diagnostics.UnreachableException>(async () => await _handler.Handle(new CreateCourseCommand(payload)));
    }

    [Test]
    public async Task Handle_PassesInsertDelegateThatInvokesCourseDaoCreate()
    {
        var payload = new CreateCourseDto { Name = "Curso Demo", ExternalReference = "ref-delegate" };
        var candidate = new Course { Id = Guid.NewGuid(), Name = "Curso Demo", TenantId = TenantId };
        var mapped = new GetCourseDto { Id = candidate.Id, Name = candidate.Name };
        var transactionContext = new Mock<ITransactionContext>(MockBehavior.Strict);

        _courseBuilder.Setup(builder => builder.BuildCourse(TenantId, payload)).Returns(candidate);
        _courseDao.Setup(dao => dao.CreateAsync(candidate, transactionContext.Object)).Returns(Task.CompletedTask);
        _mapper.Setup(map => map.Map<GetCourseDto>(candidate)).Returns(mapped);

        Func<ITransactionContext, Task<Course?>>? capturedInsert = null;
        _idempotentExecutor
            .Setup(executor => executor.ExecuteAsync<Course>(
                TenantId, "ref-delegate", "Course", candidate.Id,
                It.IsAny<Func<ITransactionContext, Task<Course?>>>(),
                It.IsAny<Func<Guid, Task<Course?>>>()))
            .Callback<Guid, string?, string, Guid, Func<ITransactionContext, Task<Course?>>, Func<Guid, Task<Course?>>>(
                (_, _, _, _, insertDelegate, _) => capturedInsert = insertDelegate)
            .ReturnsAsync(new IdempotentInsertOutcome<Course>.Inserted(candidate));

        await _handler.Handle(new CreateCourseCommand(payload));
        Course? inserted = await capturedInsert!(transactionContext.Object);

        Assert.That(inserted, Is.SameAs(candidate));
        _courseDao.Verify(dao => dao.CreateAsync(candidate, transactionContext.Object), Times.Once);
    }
}

using Backend.Application.Courses;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities;
using Backend.Results.Courses;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Courses;

[TestFixture]
public class DeleteCourseHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CourseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<ICourseDao> _courseDao = null!;
    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IOutboxEventDao> _outboxEventDao = null!;
    private Mock<ICourseEventBuilder> _courseEventBuilder = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private DeleteCourseHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        _courseEventBuilder = new Mock<ICourseEventBuilder>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new DeleteCourseHandler(
            _courseDao.Object,
            _scheduledClassDao.Object,
            _uniqueClassDao.Object,
            _unitOfWork.Object,
            _outboxEventDao.Object,
            _courseEventBuilder.Object,
            _claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDeleteReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        _scheduledClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, _transactionScope.Object)).ReturnsAsync([]);
        _uniqueClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, _transactionScope.Object)).ReturnsAsync([]);
        _courseDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, CourseId, _transactionScope.Object)).ReturnsAsync(false);

        DeleteCourseResult result = await _handler.Handle(new DeleteCourseCommand(CourseId));

        Assert.That(result, Is.InstanceOf<DeleteCourseResult.NotFound>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        _outboxEventDao.VerifyNoOtherCalls();
        _courseEventBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenDeleteSucceeds_InsertsOutboxWithAffectedClassIdsAndCommits()
    {
        var scheduledIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var uniqueIds = new List<Guid> { Guid.NewGuid() };
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            AggregateType = "Course",
            AggregateId = CourseId,
            EventType = "CourseDeleted",
            RoutingKey = "course.deleted",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow
        };

        _scheduledClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, _transactionScope.Object)).ReturnsAsync(scheduledIds);
        _uniqueClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, _transactionScope.Object)).ReturnsAsync(uniqueIds);
        _courseDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, CourseId, _transactionScope.Object)).ReturnsAsync(true);
        _courseEventBuilder
            .Setup(builder => builder.BuildCourseDeleted(
                TenantId,
                CourseId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 3 && ids[0] == scheduledIds[0] && ids[2] == uniqueIds[0])))
            .Returns(outboxEvent);
        _outboxEventDao.Setup(dao => dao.InsertAsync(outboxEvent, _transactionScope.Object)).Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        DeleteCourseResult result = await _handler.Handle(new DeleteCourseCommand(CourseId));

        Assert.That(result, Is.InstanceOf<DeleteCourseResult.Deleted>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _outboxEventDao.Verify(dao => dao.InsertAsync(outboxEvent, _transactionScope.Object), Times.Once);
    }
}

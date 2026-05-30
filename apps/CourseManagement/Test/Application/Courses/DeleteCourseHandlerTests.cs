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

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IUniqueClassDao> uniqueClassDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IOutboxEventDao> outboxEventDao = null!;
    private Mock<ICourseEventBuilder> courseEventBuilder = null!;
    private Mock<IClaimContext> claimContext = null!;
    private DeleteCourseHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        courseEventBuilder = new Mock<ICourseEventBuilder>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        handler = new DeleteCourseHandler(
            courseDao.Object,
            scheduledClassDao.Object,
            uniqueClassDao.Object,
            unitOfWork.Object,
            outboxEventDao.Object,
            courseEventBuilder.Object,
            claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDeleteReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        scheduledClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, transactionScope.Object)).ReturnsAsync([]);
        uniqueClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, transactionScope.Object)).ReturnsAsync([]);
        courseDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, CourseId, transactionScope.Object)).ReturnsAsync(false);

        DeleteCourseResult result = await handler.Handle(new DeleteCourseCommand(CourseId));

        Assert.That(result, Is.InstanceOf<DeleteCourseResult.NotFound>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        outboxEventDao.VerifyNoOtherCalls();
        courseEventBuilder.VerifyNoOtherCalls();
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

        scheduledClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, transactionScope.Object)).ReturnsAsync(scheduledIds);
        uniqueClassDao.Setup(dao => dao.GetIdsByCourseForTenantAsync(TenantId, CourseId, transactionScope.Object)).ReturnsAsync(uniqueIds);
        courseDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, CourseId, transactionScope.Object)).ReturnsAsync(true);
        courseEventBuilder
            .Setup(builder => builder.BuildCourseDeleted(
                TenantId,
                CourseId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 3 && ids[0] == scheduledIds[0] && ids[2] == uniqueIds[0])))
            .Returns(outboxEvent);
        outboxEventDao.Setup(dao => dao.InsertAsync(outboxEvent, transactionScope.Object)).Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        DeleteCourseResult result = await handler.Handle(new DeleteCourseCommand(CourseId));

        Assert.That(result, Is.InstanceOf<DeleteCourseResult.Deleted>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        outboxEventDao.Verify(dao => dao.InsertAsync(outboxEvent, transactionScope.Object), Times.Once);
    }
}

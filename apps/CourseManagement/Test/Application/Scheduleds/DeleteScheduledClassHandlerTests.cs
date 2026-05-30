using Backend.Application.Scheduleds;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Entities;
using Backend.Results.Scheduleds;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Scheduleds;

[TestFixture]
public class DeleteScheduledClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScheduledClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IOutboxEventDao> outboxEventDao = null!;
    private Mock<ICourseEventBuilder> courseEventBuilder = null!;
    private Mock<IClaimContext> claimContext = null!;
    private DeleteScheduledClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        courseEventBuilder = new Mock<ICourseEventBuilder>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        handler = new DeleteScheduledClassHandler(
            scheduledClassDao.Object,
            unitOfWork.Object,
            outboxEventDao.Object,
            courseEventBuilder.Object,
            claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDeleteReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        scheduledClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, ScheduledClassId, transactionScope.Object)).ReturnsAsync(false);

        DeleteScheduledClassResult result = await handler.Handle(new DeleteScheduledClassCommand(ScheduledClassId));

        Assert.That(result, Is.InstanceOf<DeleteScheduledClassResult.NotFound>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        outboxEventDao.VerifyNoOtherCalls();
        courseEventBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenDeleteSucceeds_InsertsOutboxAndCommits()
    {
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            AggregateType = "Class",
            AggregateId = ScheduledClassId,
            EventType = "ClassDeleted",
            RoutingKey = "class.deleted",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow
        };

        scheduledClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, ScheduledClassId, transactionScope.Object)).ReturnsAsync(true);
        courseEventBuilder.Setup(builder => builder.BuildClassDeleted(TenantId, ScheduledClassId)).Returns(outboxEvent);
        outboxEventDao.Setup(dao => dao.InsertAsync(outboxEvent, transactionScope.Object)).Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        DeleteScheduledClassResult result = await handler.Handle(new DeleteScheduledClassCommand(ScheduledClassId));

        Assert.That(result, Is.InstanceOf<DeleteScheduledClassResult.Deleted>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

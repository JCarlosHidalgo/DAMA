using Backend.Application.Uniques;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities;
using Backend.Results.Uniques;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Uniques;

[TestFixture]
public class DeleteUniqueClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UniqueClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IUniqueClassDao> uniqueClassDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IOutboxEventDao> outboxEventDao = null!;
    private Mock<ICourseEventBuilder> courseEventBuilder = null!;
    private Mock<IClaimContext> claimContext = null!;
    private DeleteUniqueClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        courseEventBuilder = new Mock<ICourseEventBuilder>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        handler = new DeleteUniqueClassHandler(
            uniqueClassDao.Object,
            unitOfWork.Object,
            outboxEventDao.Object,
            courseEventBuilder.Object,
            claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDeleteReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        uniqueClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, UniqueClassId, transactionScope.Object)).ReturnsAsync(false);

        DeleteUniqueClassResult result = await handler.Handle(new DeleteUniqueClassCommand(UniqueClassId));

        Assert.That(result, Is.InstanceOf<DeleteUniqueClassResult.NotFound>());
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
            AggregateId = UniqueClassId,
            EventType = "ClassDeleted",
            RoutingKey = "class.deleted",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow
        };

        uniqueClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, UniqueClassId, transactionScope.Object)).ReturnsAsync(true);
        courseEventBuilder.Setup(builder => builder.BuildClassDeleted(TenantId, UniqueClassId)).Returns(outboxEvent);
        outboxEventDao.Setup(dao => dao.InsertAsync(outboxEvent, transactionScope.Object)).Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        DeleteUniqueClassResult result = await handler.Handle(new DeleteUniqueClassCommand(UniqueClassId));

        Assert.That(result, Is.InstanceOf<DeleteUniqueClassResult.Deleted>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

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

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IOutboxEventDao> _outboxEventDao = null!;
    private Mock<ICourseEventBuilder> _courseEventBuilder = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private DeleteScheduledClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        _courseEventBuilder = new Mock<ICourseEventBuilder>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new DeleteScheduledClassHandler(
            _scheduledClassDao.Object,
            _unitOfWork.Object,
            _outboxEventDao.Object,
            _courseEventBuilder.Object,
            _claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDeleteReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        _scheduledClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, ScheduledClassId, _transactionScope.Object)).ReturnsAsync(false);

        DeleteScheduledClassResult result = await _handler.Handle(new DeleteScheduledClassCommand(ScheduledClassId));

        Assert.That(result, Is.InstanceOf<DeleteScheduledClassResult.NotFound>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        _outboxEventDao.VerifyNoOtherCalls();
        _courseEventBuilder.VerifyNoOtherCalls();
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

        _scheduledClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, ScheduledClassId, _transactionScope.Object)).ReturnsAsync(true);
        _courseEventBuilder.Setup(builder => builder.BuildClassDeleted(TenantId, ScheduledClassId)).Returns(outboxEvent);
        _outboxEventDao.Setup(dao => dao.InsertAsync(outboxEvent, _transactionScope.Object)).Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        DeleteScheduledClassResult result = await _handler.Handle(new DeleteScheduledClassCommand(ScheduledClassId));

        Assert.That(result, Is.InstanceOf<DeleteScheduledClassResult.Deleted>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

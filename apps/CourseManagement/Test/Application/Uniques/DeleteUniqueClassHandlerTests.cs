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

    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IOutboxEventDao> _outboxEventDao = null!;
    private Mock<ICourseEventBuilder> _courseEventBuilder = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private DeleteUniqueClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        _courseEventBuilder = new Mock<ICourseEventBuilder>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new DeleteUniqueClassHandler(
            _uniqueClassDao.Object,
            _unitOfWork.Object,
            _outboxEventDao.Object,
            _courseEventBuilder.Object,
            _claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDeleteReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        _uniqueClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, UniqueClassId, _transactionScope.Object)).ReturnsAsync(false);

        DeleteUniqueClassResult result = await _handler.Handle(new DeleteUniqueClassCommand(UniqueClassId));

        Assert.That(result, Is.InstanceOf<DeleteUniqueClassResult.NotFound>());
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
            AggregateId = UniqueClassId,
            EventType = "ClassDeleted",
            RoutingKey = "class.deleted",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow
        };

        _uniqueClassDao.Setup(dao => dao.DeleteForTenantAsync(TenantId, UniqueClassId, _transactionScope.Object)).ReturnsAsync(true);
        _courseEventBuilder.Setup(builder => builder.BuildClassDeleted(TenantId, UniqueClassId)).Returns(outboxEvent);
        _outboxEventDao.Setup(dao => dao.InsertAsync(outboxEvent, _transactionScope.Object)).Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        DeleteUniqueClassResult result = await _handler.Handle(new DeleteUniqueClassCommand(UniqueClassId));

        Assert.That(result, Is.InstanceOf<DeleteUniqueClassResult.Deleted>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

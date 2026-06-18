using Backend.Application.Uniques;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities.Uniques;
using Backend.Results.Uniques;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Uniques;

[TestFixture]
public class TransferUniqueClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UniqueClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TargetGroupId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private TransferUniqueClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new TransferUniqueClassHandler(
            _uniqueClassDao.Object,
            _classGroupDao.Object,
            _unitOfWork.Object,
            _claimContext.Object);
    }

    private static UniqueClass BuildExisting()
    {
        return new UniqueClass
        {
            Id = UniqueClassId,
            Date = new DateOnly(2026, 6, 18),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            TenantId = TenantId
        };
    }

    [Test]
    public async Task Handle_WhenUniqueClassMissing_ReturnsNotFound()
    {
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync((UniqueClass?)null);

        TransferUniqueClassResult result = await _handler.Handle(new TransferUniqueClassCommand(UniqueClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferUniqueClassResult.NotFound>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenTargetGroupMissing_ReturnsGroupNotFound()
    {
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(BuildExisting());
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(false);

        TransferUniqueClassResult result = await _handler.Handle(new TransferUniqueClassCommand(UniqueClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferUniqueClassResult.GroupNotFound>());
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflict()
    {
        UniqueClass existing = BuildExisting();
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(existing);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.Date, existing.StartTime, existing.EndTime, UniqueClassId))
            .ReturnsAsync(true);

        TransferUniqueClassResult result = await _handler.Handle(new TransferUniqueClassCommand(UniqueClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferUniqueClassResult.GroupOverlapConflict>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenTransferReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        UniqueClass existing = BuildExisting();
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(existing);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.Date, existing.StartTime, existing.EndTime, UniqueClassId))
            .ReturnsAsync(false);
        _uniqueClassDao
            .Setup(dao => dao.TransferToGroupAsync(TenantId, UniqueClassId, TargetGroupId, _transactionScope.Object))
            .ReturnsAsync(false);

        TransferUniqueClassResult result = await _handler.Handle(new TransferUniqueClassCommand(UniqueClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferUniqueClassResult.NotFound>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenTransferSucceeds_ReturnsTransferredAndCommits()
    {
        UniqueClass existing = BuildExisting();
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(existing);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.Date, existing.StartTime, existing.EndTime, UniqueClassId))
            .ReturnsAsync(false);
        _uniqueClassDao
            .Setup(dao => dao.TransferToGroupAsync(TenantId, UniqueClassId, TargetGroupId, _transactionScope.Object))
            .ReturnsAsync(true);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TransferUniqueClassResult result = await _handler.Handle(new TransferUniqueClassCommand(UniqueClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferUniqueClassResult.Transferred>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

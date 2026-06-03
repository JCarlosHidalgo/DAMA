using Backend.Application.Scheduleds;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Entities.Scheduleds;
using Backend.Results.Scheduleds;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Scheduleds;

[TestFixture]
public class TransferScheduledClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScheduledClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CurrentGroupId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TargetGroupId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private TransferScheduledClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new TransferScheduledClassHandler(
            _scheduledClassDao.Object,
            _classGroupDao.Object,
            _unitOfWork.Object,
            _claimContext.Object);
    }

    private static ScheduledClass Existing() => new()
    {
        Id = ScheduledClassId,
        TenantId = TenantId,
        GroupId = CurrentGroupId,
        DayOfWeekIndex = 3,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0)
    };

    [Test]
    public async Task Handle_WhenClassNotFound_ReturnsNotFound()
    {
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync((ScheduledClass?)null);

        TransferScheduledClassResult result = await _handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.NotFound>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenTargetGroupDoesNotExist_ReturnsGroupNotFound()
    {
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(false);

        TransferScheduledClassResult result = await _handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.GroupNotFound>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenOverlapInTargetGroup_ReturnsGroupOverlapConflict()
    {
        ScheduledClass existing = Existing();
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(existing);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.DayOfWeekIndex, existing.StartTime, existing.EndTime, ScheduledClassId))
            .ReturnsAsync(true);

        TransferScheduledClassResult result = await _handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.GroupOverlapConflict>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenValid_TransfersCommitsAndReturnsTransferred()
    {
        ScheduledClass existing = Existing();
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(existing);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.DayOfWeekIndex, existing.StartTime, existing.EndTime, ScheduledClassId))
            .ReturnsAsync(false);
        _scheduledClassDao
            .Setup(dao => dao.TransferToGroupAsync(TenantId, ScheduledClassId, TargetGroupId, _transactionScope.Object))
            .ReturnsAsync(true);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TransferScheduledClassResult result = await _handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.Transferred>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

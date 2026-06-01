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

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IClassGroupDao> classGroupDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IClaimContext> claimContext = null!;
    private TransferScheduledClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        handler = new TransferScheduledClassHandler(
            scheduledClassDao.Object,
            classGroupDao.Object,
            unitOfWork.Object,
            claimContext.Object);
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
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync((ScheduledClass?)null);

        TransferScheduledClassResult result = await handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.NotFound>());
        unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenTargetGroupDoesNotExist_ReturnsGroupNotFound()
    {
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(false);

        TransferScheduledClassResult result = await handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.GroupNotFound>());
        unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenOverlapInTargetGroup_ReturnsGroupOverlapConflict()
    {
        ScheduledClass existing = Existing();
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(existing);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.DayOfWeekIndex, existing.StartTime, existing.EndTime, ScheduledClassId))
            .ReturnsAsync(true);

        TransferScheduledClassResult result = await handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.GroupOverlapConflict>());
        unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenValid_TransfersCommitsAndReturnsTransferred()
    {
        ScheduledClass existing = Existing();
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(existing);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, TargetGroupId)).ReturnsAsync(true);
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, TargetGroupId, existing.DayOfWeekIndex, existing.StartTime, existing.EndTime, ScheduledClassId))
            .ReturnsAsync(false);
        scheduledClassDao
            .Setup(dao => dao.TransferToGroupAsync(TenantId, ScheduledClassId, TargetGroupId, transactionScope.Object))
            .ReturnsAsync(true);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TransferScheduledClassResult result = await handler.Handle(new TransferScheduledClassCommand(ScheduledClassId, TargetGroupId));

        Assert.That(result, Is.InstanceOf<TransferScheduledClassResult.Transferred>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

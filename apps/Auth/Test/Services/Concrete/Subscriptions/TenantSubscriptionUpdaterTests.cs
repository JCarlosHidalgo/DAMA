using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Entities.Tenants;
using Backend.Services.Concrete.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Services.Concrete.Subscriptions;

[TestFixture]
public class TenantSubscriptionUpdaterTests
{
    private Mock<ITenantAllowedServicesDao> tenantAllowedServicesDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private TenantSubscriptionUpdater sut = null!;

    [SetUp]
    public void SetUp()
    {
        tenantAllowedServicesDao = new Mock<ITenantAllowedServicesDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(transactionScope.Object);

        sut = new TenantSubscriptionUpdater(tenantAllowedServicesDao.Object, unitOfWork.Object);
    }

    [Test]
    public async Task UpdateAsync_UpsertsAllowedServicesAndCommits()
    {
        Guid tenantId = Guid.NewGuid();
        DateTime expiresAt = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);
        tenantAllowedServicesDao
            .Setup(dao => dao.UpsertAsync(
                It.Is<TenantAllowedServices>(allowed =>
                    allowed.Id == tenantId
                    && allowed.IndexCoreServicesPyramid == 3
                    && allowed.ExpiresAt == expiresAt),
                transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        await sut.UpdateAsync(tenantId, 3, expiresAt);

        tenantAllowedServicesDao.Verify(
            dao => dao.UpsertAsync(It.IsAny<TenantAllowedServices>(), transactionScope.Object), Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

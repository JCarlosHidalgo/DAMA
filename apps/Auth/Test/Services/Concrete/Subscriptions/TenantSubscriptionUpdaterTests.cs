using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Entities.Tenants;
using Backend.Services.Concrete.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Services.Concrete.Subscriptions;

[TestFixture]
public class TenantSubscriptionUpdaterTests
{
    private Mock<ITenantAllowedServicesDao> _tenantAllowedServicesDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private TenantSubscriptionUpdater _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tenantAllowedServicesDao = new Mock<ITenantAllowedServicesDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(_transactionScope.Object);

        _sut = new TenantSubscriptionUpdater(_tenantAllowedServicesDao.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task UpdateAsync_UpsertsAllowedServicesAndCommits()
    {
        Guid tenantId = Guid.NewGuid();
        DateTime expiresAt = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);
        _tenantAllowedServicesDao
            .Setup(dao => dao.UpsertAsync(
                It.Is<TenantAllowedServices>(allowed =>
                    allowed.Id == tenantId
                    && allowed.IndexCoreServicesPyramid == 3
                    && allowed.ExpiresAt == expiresAt),
                _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        await _sut.UpdateAsync(tenantId, 3, expiresAt);

        _tenantAllowedServicesDao.Verify(
            dao => dao.UpsertAsync(It.IsAny<TenantAllowedServices>(), _transactionScope.Object), Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}

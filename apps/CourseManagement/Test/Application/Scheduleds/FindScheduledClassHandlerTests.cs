using Backend.Application.Scheduleds;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Results;
using Backend.Results.Scheduleds;

using Moq;

namespace Test.Application.Scheduleds;

[TestFixture]
public class FindScheduledClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly ClassDate = new(2025, 6, 1);

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private FindScheduledClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new FindScheduledClassHandler(_scheduledClassDao.Object, _claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDaoReturnsNull_ReturnsNotFound()
    {
        _scheduledClassDao.Setup(dao => dao.FindForTenantAsync(TenantId, ClassId, ClassDate)).ReturnsAsync((ClassExistenceMeta?)null);

        FindScheduledClassResult result = await _handler.Handle(new FindScheduledClassQuery(ClassId, ClassDate));

        Assert.That(result, Is.InstanceOf<FindScheduledClassResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenDaoReturnsMeta_ReturnsFoundWithMeta()
    {
        var meta = new ClassExistenceMeta(new TimeOnly(9, 0), new TimeOnly(10, 0), null, 25);
        _scheduledClassDao.Setup(dao => dao.FindForTenantAsync(TenantId, ClassId, ClassDate)).ReturnsAsync(meta);

        FindScheduledClassResult result = await _handler.Handle(new FindScheduledClassQuery(ClassId, ClassDate));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<FindScheduledClassResult.Found>());
            Assert.That(((FindScheduledClassResult.Found)result).Meta, Is.SameAs(meta));
        });
    }
}

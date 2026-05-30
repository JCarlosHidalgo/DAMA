using Backend.Application.Uniques;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Results;
using Backend.Results.Uniques;

using Moq;

namespace Test.Application.Uniques;

[TestFixture]
public class FindUniqueClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IUniqueClassDao> uniqueClassDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private FindUniqueClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new FindUniqueClassHandler(uniqueClassDao.Object, claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenDaoReturnsNull_ReturnsNotFound()
    {
        uniqueClassDao.Setup(dao => dao.FindForTenantAsync(TenantId, ClassId)).ReturnsAsync((ClassExistenceMeta?)null);

        FindUniqueClassResult result = await handler.Handle(new FindUniqueClassQuery(ClassId));

        Assert.That(result, Is.InstanceOf<FindUniqueClassResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenDaoReturnsMeta_ReturnsFoundWithMeta()
    {
        var meta = new ClassExistenceMeta(new TimeOnly(9, 0), new TimeOnly(10, 0), new DateOnly(2025, 6, 1), 25);
        uniqueClassDao.Setup(dao => dao.FindForTenantAsync(TenantId, ClassId)).ReturnsAsync(meta);

        FindUniqueClassResult result = await handler.Handle(new FindUniqueClassQuery(ClassId));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<FindUniqueClassResult.Found>());
            Assert.That(((FindUniqueClassResult.Found)result).Meta, Is.SameAs(meta));
        });
    }
}

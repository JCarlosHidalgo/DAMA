using Backend.Application.Groups;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Results.Groups;

using Moq;

namespace Test.Application.Groups;

[TestFixture]
public class DeleteClassGroupHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GroupId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private DeleteClassGroupHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new DeleteClassGroupHandler(_classGroupDao.Object, _claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenGroupDoesNotExist_ReturnsNotFound()
    {
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(false);

        DeleteClassGroupResult result = await _handler.Handle(new DeleteClassGroupCommand(GroupId));

        Assert.That(result, Is.InstanceOf<DeleteClassGroupResult.NotFound>());
        _classGroupDao.Verify(dao => dao.DeleteForTenantIfEmptyAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenGroupHasClasses_ReturnsGroupNotEmpty()
    {
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _classGroupDao.Setup(dao => dao.DeleteForTenantIfEmptyAsync(TenantId, GroupId)).ReturnsAsync(false);

        DeleteClassGroupResult result = await _handler.Handle(new DeleteClassGroupCommand(GroupId));

        Assert.That(result, Is.InstanceOf<DeleteClassGroupResult.GroupNotEmpty>());
    }

    [Test]
    public async Task Handle_WhenGroupEmpty_ReturnsDeleted()
    {
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _classGroupDao.Setup(dao => dao.DeleteForTenantIfEmptyAsync(TenantId, GroupId)).ReturnsAsync(true);

        DeleteClassGroupResult result = await _handler.Handle(new DeleteClassGroupCommand(GroupId));

        Assert.That(result, Is.InstanceOf<DeleteClassGroupResult.Deleted>());
    }
}

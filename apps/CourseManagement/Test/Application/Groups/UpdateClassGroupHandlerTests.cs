using Backend.Application.Groups;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Input;
using Backend.Results.Groups;

using Moq;

namespace Test.Application.Groups;

[TestFixture]
public class UpdateClassGroupHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GroupId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private UpdateClassGroupHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new UpdateClassGroupHandler(_classGroupDao.Object, _claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenUpdateSucceeds_ReturnsUpdated()
    {
        var payload = new UpdateClassGroupDto { Name = "Nuevo Nombre" };
        _classGroupDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, GroupId, payload.Name)).ReturnsAsync(true);

        UpdateClassGroupResult result = await _handler.Handle(new UpdateClassGroupCommand(GroupId, payload));

        Assert.That(result, Is.InstanceOf<UpdateClassGroupResult.Updated>());
    }

    [Test]
    public async Task Handle_WhenUpdateFails_ReturnsNotFound()
    {
        var payload = new UpdateClassGroupDto { Name = "Nuevo Nombre" };
        _classGroupDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, GroupId, payload.Name)).ReturnsAsync(false);

        UpdateClassGroupResult result = await _handler.Handle(new UpdateClassGroupCommand(GroupId, payload));

        Assert.That(result, Is.InstanceOf<UpdateClassGroupResult.NotFound>());
    }
}

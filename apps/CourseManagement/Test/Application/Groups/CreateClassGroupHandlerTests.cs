using AutoMapper;

using Backend.Application.Groups;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Input;
using Backend.Dtos.Groups.Output;
using Backend.Entities.Groups;
using Backend.Results.Groups;

using Moq;

namespace Test.Application.Groups;

[TestFixture]
public class CreateClassGroupHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IClassGroupBuilder> _classGroupBuilder = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IMapper> _mapper = null!;
    private CreateClassGroupHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _classGroupBuilder = new Mock<IClassGroupBuilder>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new CreateClassGroupHandler(
            _classGroupDao.Object,
            _classGroupBuilder.Object,
            _claimContext.Object,
            _mapper.Object);
    }

    [Test]
    public async Task Handle_WhenCreated_ReturnsCreatedWithMappedDto()
    {
        var payload = new CreateClassGroupDto { Name = "Grupo Demo" };
        var classGroup = new ClassGroup { Id = Guid.NewGuid(), Name = "Grupo Demo", TenantId = TenantId };
        var dto = new GetClassGroupDto { Id = classGroup.Id, Name = classGroup.Name };

        _classGroupBuilder.Setup(builder => builder.BuildClassGroup(TenantId, payload)).Returns(classGroup);
        _classGroupDao.Setup(dao => dao.CreateForTenantAsync(classGroup)).Returns(Task.CompletedTask);
        _mapper.Setup(mapper => mapper.Map<GetClassGroupDto>(classGroup)).Returns(dto);

        CreateClassGroupResult result = await _handler.Handle(new CreateClassGroupCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateClassGroupResult.Created>());
        Assert.That(((CreateClassGroupResult.Created)result).Group, Is.SameAs(dto));
    }
}

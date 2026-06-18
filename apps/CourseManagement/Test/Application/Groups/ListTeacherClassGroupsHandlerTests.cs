using AutoMapper;

using Backend.Application.Groups;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Output;
using Backend.Entities.Groups;
using Backend.Results.Groups;

using Moq;

namespace Test.Application.Groups;

[TestFixture]
public class ListTeacherClassGroupsHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TeacherId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IMapper> _mapper = null!;
    private ListTeacherClassGroupsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _claimContext.SetupGet(context => context.UserId).Returns(TeacherId);

        _handler = new ListTeacherClassGroupsHandler(_classGroupDao.Object, _claimContext.Object, _mapper.Object);
    }

    [Test]
    public async Task Handle_WhenGroupsExist_ReturnsFoundWithMappedDtos()
    {
        var groups = new List<ClassGroup>
        {
            new ClassGroup { Id = Guid.NewGuid(), Name = "Grupo A", TenantId = TenantId }
        };
        var dtos = new List<GetClassGroupDto>
        {
            new GetClassGroupDto { Id = groups[0].Id, Name = groups[0].Name }
        };

        _classGroupDao.Setup(dao => dao.GetByTeacherForTenantAsync(TenantId, TeacherId)).ReturnsAsync(groups);
        _mapper.Setup(mapper => mapper.Map<List<ClassGroup>, List<GetClassGroupDto>>(groups)).Returns(dtos);

        ListClassGroupsResult result = await _handler.Handle(new ListTeacherClassGroupsQuery());

        Assert.That(result, Is.InstanceOf<ListClassGroupsResult.Found>());
        Assert.That(((ListClassGroupsResult.Found)result).Groups, Is.SameAs(dtos));
    }
}

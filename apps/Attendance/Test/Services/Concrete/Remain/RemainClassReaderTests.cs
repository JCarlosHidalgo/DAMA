using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Dtos.Remain.Output;
using Backend.Entities.Remain;
using Backend.Results.Remain;
using Backend.Security;
using Backend.Services.Concrete.Remain;

using Moq;

namespace Test.Services.Concrete.Remain;

[TestFixture]
public class RemainClassReaderTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CallerStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IStudentRemainClassesDao> _remainClassesDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IRemainClassBuilder> _remainClassBuilder = null!;
    private Mock<IMapper> _mapper = null!;

    private RemainClassReader _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _remainClassBuilder = new Mock<IRemainClassBuilder>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _claimContext.Setup(target => target.TenantId).Returns(CallerTenantId);

        _sut = new RemainClassReader(
            _remainClassesDao.Object,
            _claimContext.Object,
            _remainClassBuilder.Object,
            _mapper.Object);
    }

    [Test]
    public async Task GetForStudentAsync_WhenStudentAccessingOther_ReturnsForbidden()
    {
        var studentId = Guid.NewGuid();
        _claimContext.Setup(target => target.Role).Returns(UserRoles.Student);
        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);

        GetRemainForStudentOutcome result = await _sut.GetForStudentAsync(studentId);

        Assert.That(result, Is.InstanceOf<GetRemainForStudentOutcome.Forbidden>());
        _remainClassesDao.Verify(target => target.ReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetForStudentAsync_WhenRemainExists_ReturnsFoundWithMappedDto()
    {
        var studentId = Guid.NewGuid();
        StudentRemainClasses remainClasses = new() { TenantId = CallerTenantId, Id = studentId, NumberOfClasses = 5 };
        RemainResponse expectedResponse = new() { StudentId = studentId, NumberOfClasses = 5 };

        _claimContext.Setup(target => target.Role).Returns(UserRoles.Client);
        _remainClassesDao.Setup(target => target.ReadAsync(CallerTenantId, studentId)).ReturnsAsync(remainClasses);
        _mapper.Setup(target => target.Map<RemainResponse>(remainClasses)).Returns(expectedResponse);

        GetRemainForStudentOutcome result = await _sut.GetForStudentAsync(studentId);

        Assert.That(result, Is.InstanceOf<GetRemainForStudentOutcome.Found>());
        var found = (GetRemainForStudentOutcome.Found)result;
        Assert.That(found.Remain, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GetForStudentAsync_WhenRemainMissing_BuildsEmptyAndReturnsFound()
    {
        var studentId = Guid.NewGuid();
        StudentRemainClasses emptyRemain = new() { TenantId = CallerTenantId, Id = studentId, NumberOfClasses = 0 };
        RemainResponse expectedResponse = new() { StudentId = studentId, NumberOfClasses = 0 };

        _claimContext.Setup(target => target.Role).Returns(UserRoles.Client);
        _remainClassesDao.Setup(target => target.ReadAsync(CallerTenantId, studentId)).ReturnsAsync((StudentRemainClasses?)null);
        _remainClassBuilder.Setup(target => target.BuildEmptyRemain(CallerTenantId, studentId)).Returns(emptyRemain);
        _mapper.Setup(target => target.Map<RemainResponse>(emptyRemain)).Returns(expectedResponse);

        GetRemainForStudentOutcome result = await _sut.GetForStudentAsync(studentId);

        Assert.That(result, Is.InstanceOf<GetRemainForStudentOutcome.Found>());
    }

    [Test]
    public async Task GetForCurrentStudentAsync_WhenRemainExists_ReturnsMappedDto()
    {
        StudentRemainClasses remainClasses = new() { TenantId = CallerTenantId, Id = CallerStudentId, NumberOfClasses = 3 };
        RemainResponse expectedResponse = new() { StudentId = CallerStudentId, NumberOfClasses = 3 };

        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _remainClassesDao.Setup(target => target.ReadAsync(CallerTenantId, CallerStudentId)).ReturnsAsync(remainClasses);
        _mapper.Setup(target => target.Map<RemainResponse>(remainClasses)).Returns(expectedResponse);

        RemainResponse result = await _sut.GetForCurrentStudentAsync();

        Assert.That(result, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GetForCurrentStudentAsync_WhenRemainMissing_BuildsEmpty()
    {
        StudentRemainClasses empty = new() { TenantId = CallerTenantId, Id = CallerStudentId };
        RemainResponse emptyResponse = new() { StudentId = CallerStudentId };

        _claimContext.Setup(target => target.UserId).Returns(CallerStudentId);
        _remainClassesDao.Setup(target => target.ReadAsync(CallerTenantId, CallerStudentId)).ReturnsAsync((StudentRemainClasses?)null);
        _remainClassBuilder.Setup(target => target.BuildEmptyRemain(CallerTenantId, CallerStudentId)).Returns(empty);
        _mapper.Setup(target => target.Map<RemainResponse>(empty)).Returns(emptyResponse);

        RemainResponse result = await _sut.GetForCurrentStudentAsync();

        Assert.That(result, Is.SameAs(emptyResponse));
    }
}

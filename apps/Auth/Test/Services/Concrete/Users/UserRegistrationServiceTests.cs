using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.DB.Daos.Abstract.TwoForeign.Tenants;
using Backend.Dtos.Users.Input;
using Backend.Entities;
using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Results.Users;
using Backend.Services.Concrete.Users;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Services.Concrete.Users;

[TestFixture]
public class UserRegistrationServiceTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Mock<IUserRegistrationDao> _userRegistrationDao = null!;
    private Mock<ITenantDomainDao> _tenantDomainDao = null!;
    private Mock<IOutboxEventDao> _outboxEventDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IStudentRegisteredEventBuilder> _studentRegisteredEventBuilder = null!;
    private Mock<IUserEntityBuilder> _userEntityBuilder = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private UserRegistrationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRegistrationDao = new Mock<IUserRegistrationDao>(MockBehavior.Strict);
        _tenantDomainDao = new Mock<ITenantDomainDao>(MockBehavior.Strict);
        _outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _studentRegisteredEventBuilder = new Mock<IStudentRegisteredEventBuilder>(MockBehavior.Strict);
        _userEntityBuilder = new Mock<IUserEntityBuilder>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);

        _sut = new UserRegistrationService(
            _userRegistrationDao.Object,
            _tenantDomainDao.Object,
            _outboxEventDao.Object,
            _unitOfWork.Object,
            _claimContext.Object,
            _studentRegisteredEventBuilder.Object,
            _userEntityBuilder.Object);
    }

    [Test]
    public async Task RegisterAsync_WhenStudentRoleAndCreationSucceeds_CommitsAndPublishesOutboxEvent()
    {
        RegisterCredentialsDto request = new() { Username = "valid_student", Password = "valid_pass" };
        User builtUser = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            PasswordHash = "hashed",
            Role = UserRole.Student.Value
        };
        TenantDomain builtTenantDomain = new() { UserId = builtUser.Id, TenantId = CallerTenantId };
        OutboxEvent builtOutboxEvent = new()
        {
            Id = Guid.NewGuid(),
            AggregateType = "Student",
            AggregateId = builtUser.Id,
            EventType = "StudentRegistered",
            RoutingKey = "student.registered",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow
        };

        _userEntityBuilder.Setup(builder => builder.BuildUser(request, UserRole.Student)).Returns(builtUser);
        _userEntityBuilder.Setup(builder => builder.BuildTenantDomain(builtUser.Id, CallerTenantId)).Returns(builtTenantDomain);
        _userRegistrationDao.Setup(dao => dao.TryCreateAsync(builtUser, _transactionScope.Object)).ReturnsAsync(true);
        _tenantDomainDao.Setup(dao => dao.CreateAsync(builtTenantDomain, _transactionScope.Object)).Returns(Task.CompletedTask);
        _studentRegisteredEventBuilder.Setup(builder => builder.Build(builtUser, CallerTenantId)).Returns(builtOutboxEvent);
        _outboxEventDao.Setup(dao => dao.InsertAsync(builtOutboxEvent, _transactionScope.Object)).Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        RegisterUserOutcome outcome = await _sut.RegisterAsync(request, UserRole.Student);

        Assert.That(outcome, Is.InstanceOf<RegisterUserOutcome.Created>());
        _userEntityBuilder.Verify(builder => builder.BuildUser(request, UserRole.Student), Times.Once);
        _userRegistrationDao.Verify(dao => dao.TryCreateAsync(builtUser, _transactionScope.Object), Times.Once);
        _userEntityBuilder.Verify(builder => builder.BuildTenantDomain(builtUser.Id, CallerTenantId), Times.Once);
        _tenantDomainDao.Verify(dao => dao.CreateAsync(builtTenantDomain, _transactionScope.Object), Times.Once);
        _studentRegisteredEventBuilder.Verify(builder => builder.Build(builtUser, CallerTenantId), Times.Once);
        _outboxEventDao.Verify(dao => dao.InsertAsync(builtOutboxEvent, _transactionScope.Object), Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_WhenTeacherRoleAndCreationSucceeds_CommitsWithoutOutboxEvent()
    {
        RegisterCredentialsDto request = new() { Username = "valid_teacher", Password = "valid_pass" };
        User builtUser = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            PasswordHash = "hashed",
            Role = UserRole.Teacher.Value
        };
        TenantDomain builtTenantDomain = new() { UserId = builtUser.Id, TenantId = CallerTenantId };

        _userEntityBuilder.Setup(builder => builder.BuildUser(request, UserRole.Teacher)).Returns(builtUser);
        _userEntityBuilder.Setup(builder => builder.BuildTenantDomain(builtUser.Id, CallerTenantId)).Returns(builtTenantDomain);
        _userRegistrationDao.Setup(dao => dao.TryCreateAsync(builtUser, _transactionScope.Object)).ReturnsAsync(true);
        _tenantDomainDao.Setup(dao => dao.CreateAsync(builtTenantDomain, _transactionScope.Object)).Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        RegisterUserOutcome outcome = await _sut.RegisterAsync(request, UserRole.Teacher);

        Assert.That(outcome, Is.InstanceOf<RegisterUserOutcome.Created>());
        _tenantDomainDao.Verify(dao => dao.CreateAsync(builtTenantDomain, _transactionScope.Object), Times.Once);
        _studentRegisteredEventBuilder.Verify(builder => builder.Build(It.IsAny<User>(), It.IsAny<Guid>()), Times.Never);
        _outboxEventDao.Verify(dao => dao.InsertAsync(It.IsAny<OutboxEvent>(), It.IsAny<ITransactionContext>()), Times.Never);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_WhenUserNameDuplicated_ReturnsDuplicateNameAndDoesNotCommit()
    {
        RegisterCredentialsDto request = new() { Username = "taken_name", Password = "valid_pass" };
        User builtUser = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            PasswordHash = "hashed",
            Role = UserRole.Student.Value
        };

        _userEntityBuilder.Setup(builder => builder.BuildUser(request, UserRole.Student)).Returns(builtUser);
        _userRegistrationDao.Setup(dao => dao.TryCreateAsync(builtUser, _transactionScope.Object)).ReturnsAsync(false);

        RegisterUserOutcome outcome = await _sut.RegisterAsync(request, UserRole.Student);

        Assert.That(outcome, Is.InstanceOf<RegisterUserOutcome.DuplicateName>());
        _userEntityBuilder.Verify(builder => builder.BuildTenantDomain(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _tenantDomainDao.Verify(dao => dao.CreateAsync(It.IsAny<TenantDomain>(), It.IsAny<ITransactionContext>()), Times.Never);
        _studentRegisteredEventBuilder.Verify(builder => builder.Build(It.IsAny<User>(), It.IsAny<Guid>()), Times.Never);
        _outboxEventDao.Verify(dao => dao.InsertAsync(It.IsAny<OutboxEvent>(), It.IsAny<ITransactionContext>()), Times.Never);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        _transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
    }
}

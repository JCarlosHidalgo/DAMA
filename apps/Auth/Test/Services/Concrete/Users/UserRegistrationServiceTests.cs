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

    private Mock<IUserRegistrationDao> userRegistrationDao = null!;
    private Mock<ITenantDomainDao> tenantDomainDao = null!;
    private Mock<IOutboxEventDao> outboxEventDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IStudentRegisteredEventBuilder> studentRegisteredEventBuilder = null!;
    private Mock<IUserEntityBuilder> userEntityBuilder = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private UserRegistrationService sut = null!;

    [SetUp]
    public void SetUp()
    {
        userRegistrationDao = new Mock<IUserRegistrationDao>(MockBehavior.Strict);
        tenantDomainDao = new Mock<ITenantDomainDao>(MockBehavior.Strict);
        outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        studentRegisteredEventBuilder = new Mock<IStudentRegisteredEventBuilder>(MockBehavior.Strict);
        userEntityBuilder = new Mock<IUserEntityBuilder>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);

        sut = new UserRegistrationService(
            userRegistrationDao.Object,
            tenantDomainDao.Object,
            outboxEventDao.Object,
            unitOfWork.Object,
            claimContext.Object,
            studentRegisteredEventBuilder.Object,
            userEntityBuilder.Object);
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

        userEntityBuilder.Setup(builder => builder.BuildUser(request, UserRole.Student)).Returns(builtUser);
        userEntityBuilder.Setup(builder => builder.BuildTenantDomain(builtUser.Id, CallerTenantId)).Returns(builtTenantDomain);
        userRegistrationDao.Setup(dao => dao.TryCreateAsync(builtUser, transactionScope.Object)).ReturnsAsync(true);
        tenantDomainDao.Setup(dao => dao.CreateAsync(builtTenantDomain, transactionScope.Object)).Returns(Task.CompletedTask);
        studentRegisteredEventBuilder.Setup(builder => builder.Build(builtUser, CallerTenantId)).Returns(builtOutboxEvent);
        outboxEventDao.Setup(dao => dao.InsertAsync(builtOutboxEvent, transactionScope.Object)).Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        RegisterUserOutcome outcome = await sut.RegisterAsync(request, UserRole.Student);

        Assert.That(outcome, Is.InstanceOf<RegisterUserOutcome.Created>());
        userEntityBuilder.Verify(builder => builder.BuildUser(request, UserRole.Student), Times.Once);
        userRegistrationDao.Verify(dao => dao.TryCreateAsync(builtUser, transactionScope.Object), Times.Once);
        userEntityBuilder.Verify(builder => builder.BuildTenantDomain(builtUser.Id, CallerTenantId), Times.Once);
        tenantDomainDao.Verify(dao => dao.CreateAsync(builtTenantDomain, transactionScope.Object), Times.Once);
        studentRegisteredEventBuilder.Verify(builder => builder.Build(builtUser, CallerTenantId), Times.Once);
        outboxEventDao.Verify(dao => dao.InsertAsync(builtOutboxEvent, transactionScope.Object), Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
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

        userEntityBuilder.Setup(builder => builder.BuildUser(request, UserRole.Teacher)).Returns(builtUser);
        userEntityBuilder.Setup(builder => builder.BuildTenantDomain(builtUser.Id, CallerTenantId)).Returns(builtTenantDomain);
        userRegistrationDao.Setup(dao => dao.TryCreateAsync(builtUser, transactionScope.Object)).ReturnsAsync(true);
        tenantDomainDao.Setup(dao => dao.CreateAsync(builtTenantDomain, transactionScope.Object)).Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        RegisterUserOutcome outcome = await sut.RegisterAsync(request, UserRole.Teacher);

        Assert.That(outcome, Is.InstanceOf<RegisterUserOutcome.Created>());
        tenantDomainDao.Verify(dao => dao.CreateAsync(builtTenantDomain, transactionScope.Object), Times.Once);
        studentRegisteredEventBuilder.Verify(builder => builder.Build(It.IsAny<User>(), It.IsAny<Guid>()), Times.Never);
        outboxEventDao.Verify(dao => dao.InsertAsync(It.IsAny<OutboxEvent>(), It.IsAny<ITransactionContext>()), Times.Never);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
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

        userEntityBuilder.Setup(builder => builder.BuildUser(request, UserRole.Student)).Returns(builtUser);
        userRegistrationDao.Setup(dao => dao.TryCreateAsync(builtUser, transactionScope.Object)).ReturnsAsync(false);

        RegisterUserOutcome outcome = await sut.RegisterAsync(request, UserRole.Student);

        Assert.That(outcome, Is.InstanceOf<RegisterUserOutcome.DuplicateName>());
        userEntityBuilder.Verify(builder => builder.BuildTenantDomain(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        tenantDomainDao.Verify(dao => dao.CreateAsync(It.IsAny<TenantDomain>(), It.IsAny<ITransactionContext>()), Times.Never);
        studentRegisteredEventBuilder.Verify(builder => builder.Build(It.IsAny<User>(), It.IsAny<Guid>()), Times.Never);
        outboxEventDao.Verify(dao => dao.InsertAsync(It.IsAny<OutboxEvent>(), It.IsAny<ITransactionContext>()), Times.Never);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
    }
}

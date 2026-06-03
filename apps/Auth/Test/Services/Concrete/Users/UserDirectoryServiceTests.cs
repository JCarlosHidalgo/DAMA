using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Output;
using Backend.Entities.Users;
using Backend.Results.Users;
using Backend.Security;
using Backend.Services.Concrete.Users;

using Moq;

namespace Test.Services.Concrete.Users;

[TestFixture]
public class UserDirectoryServiceTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CallerUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IUserDirectoryDao> userDirectoryDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IUserViewBuilder> userViewBuilder = null!;

    private UserDirectoryService sut = null!;

    [SetUp]
    public void SetUp()
    {
        userDirectoryDao = new Mock<IUserDirectoryDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        userViewBuilder = new Mock<IUserViewBuilder>(MockBehavior.Strict);

        claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);
        claimContext.Setup(accessor => accessor.UserId).Returns(CallerUserId);

        sut = new UserDirectoryService(
            userDirectoryDao.Object,
            claimContext.Object,
            userViewBuilder.Object);
    }

    [Test]
    public async Task DeleteUserAsync_WhenTargetIsCaller_ReturnsSelfDeleteForbidden()
    {
        DeleteUserOutcome outcome = await sut.DeleteUserAsync(CallerUserId);

        Assert.That(outcome, Is.InstanceOf<DeleteUserOutcome.SelfDeleteForbidden>());
        userDirectoryDao.Verify(
            dao => dao.GetByIdForTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
        userDirectoryDao.Verify(
            dao => dao.SoftDeleteForTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Test]
    public async Task DeleteUserAsync_WhenTargetNotFound_ReturnsNotFound()
    {
        var targetUserId = Guid.NewGuid();
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync((User?)null);

        DeleteUserOutcome outcome = await sut.DeleteUserAsync(targetUserId);

        Assert.That(outcome, Is.InstanceOf<DeleteUserOutcome.NotFound>());
        userDirectoryDao.Verify(
            dao => dao.SoftDeleteForTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Test]
    public async Task DeleteUserAsync_WhenTargetIsClient_ReturnsClientDeleteForbidden()
    {
        var targetUserId = Guid.NewGuid();
        User clientUser = new()
        {
            Id = targetUserId,
            UserName = "another_client",
            Role = UserRoles.Client
        };
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(clientUser);

        DeleteUserOutcome outcome = await sut.DeleteUserAsync(targetUserId);

        Assert.That(outcome, Is.InstanceOf<DeleteUserOutcome.ClientDeleteForbidden>());
        userDirectoryDao.Verify(
            dao => dao.SoftDeleteForTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Test]
    public async Task DeleteUserAsync_WhenSoftDeleteAffectsRows_ReturnsDeleted()
    {
        var targetUserId = Guid.NewGuid();
        User targetUser = new() { Id = targetUserId, UserName = "some_student", Role = UserRoles.Student };
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(targetUser);
        userDirectoryDao
            .Setup(dao => dao.SoftDeleteForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(1);

        DeleteUserOutcome outcome = await sut.DeleteUserAsync(targetUserId);

        Assert.That(outcome, Is.InstanceOf<DeleteUserOutcome.Deleted>());
    }

    [Test]
    public async Task DeleteUserAsync_WhenSoftDeleteAffectsZeroRows_ReturnsNotFound()
    {
        var targetUserId = Guid.NewGuid();
        User targetUser = new() { Id = targetUserId, UserName = "some_student", Role = UserRoles.Student };
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(targetUser);
        userDirectoryDao
            .Setup(dao => dao.SoftDeleteForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(0);

        DeleteUserOutcome outcome = await sut.DeleteUserAsync(targetUserId);

        Assert.That(outcome, Is.InstanceOf<DeleteUserOutcome.NotFound>());
    }

    [Test]
    public async Task RenameUserAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var targetUserId = Guid.NewGuid();
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync((User?)null);

        RenameUserOutcome outcome = await sut.RenameUserAsync(targetUserId, "new_name");

        Assert.That(outcome, Is.InstanceOf<RenameUserOutcome.NotFound>());
        userDirectoryDao.Verify(
            dao => dao.TryUpdateUserNameForTenantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task RenameUserAsync_WhenNewNameDuplicated_ReturnsDuplicateName()
    {
        var targetUserId = Guid.NewGuid();
        User targetUser = new() { Id = targetUserId, UserName = "old_name", Role = UserRoles.Student };
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(targetUser);
        userDirectoryDao
            .Setup(dao => dao.TryUpdateUserNameForTenantAsync(targetUserId, CallerTenantId, "taken_name"))
            .ReturnsAsync(-1);

        RenameUserOutcome outcome = await sut.RenameUserAsync(targetUserId, "taken_name");

        Assert.That(outcome, Is.InstanceOf<RenameUserOutcome.DuplicateName>());
    }

    [Test]
    public async Task RenameUserAsync_WhenUpdateAffectsZeroRows_ReturnsNotFound()
    {
        var targetUserId = Guid.NewGuid();
        User targetUser = new() { Id = targetUserId, UserName = "old_name", Role = UserRoles.Student };
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(targetUser);
        userDirectoryDao
            .Setup(dao => dao.TryUpdateUserNameForTenantAsync(targetUserId, CallerTenantId, "new_name"))
            .ReturnsAsync(0);

        RenameUserOutcome outcome = await sut.RenameUserAsync(targetUserId, "new_name");

        Assert.That(outcome, Is.InstanceOf<RenameUserOutcome.NotFound>());
    }

    [Test]
    public async Task RenameUserAsync_WhenUpdateSucceeds_ReturnsRenamed()
    {
        var targetUserId = Guid.NewGuid();
        User targetUser = new() { Id = targetUserId, UserName = "old_name", Role = UserRoles.Student };
        userDirectoryDao
            .Setup(dao => dao.GetByIdForTenantAsync(targetUserId, CallerTenantId))
            .ReturnsAsync(targetUser);
        userDirectoryDao
            .Setup(dao => dao.TryUpdateUserNameForTenantAsync(targetUserId, CallerTenantId, "new_name"))
            .ReturnsAsync(1);

        RenameUserOutcome outcome = await sut.RenameUserAsync(targetUserId, "new_name");

        Assert.That(outcome, Is.InstanceOf<RenameUserOutcome.Renamed>());
    }

    [Test]
    public async Task FindStudentByExactNameAsync_WhenStudentExists_ReturnsBuiltListItem()
    {
        User foundStudent = new()
        {
            Id = Guid.NewGuid(),
            UserName = "exact_match_name",
            Role = UserRoles.Student
        };
        UserListItemDto builtListItem = new() { Id = foundStudent.Id, Username = foundStudent.UserName };
        userDirectoryDao
            .Setup(dao => dao.GetStudentByExactNameForTenantAsync(CallerTenantId, "exact_match_name"))
            .ReturnsAsync(foundStudent);
        userViewBuilder
            .Setup(builder => builder.BuildUserListItem(foundStudent))
            .Returns(builtListItem);

        UserListItemDto? listItem = await sut.FindStudentByExactNameAsync("exact_match_name");

        Assert.That(listItem, Is.SameAs(builtListItem));
    }

    [Test]
    public async Task FindStudentByExactNameAsync_WhenStudentNotFound_ReturnsNull()
    {
        userDirectoryDao
            .Setup(dao => dao.GetStudentByExactNameForTenantAsync(CallerTenantId, "no_such_name"))
            .ReturnsAsync((User?)null);

        UserListItemDto? listItem = await sut.FindStudentByExactNameAsync("no_such_name");

        Assert.That(listItem, Is.Null);
        userViewBuilder.Verify(builder => builder.BuildUserListItem(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task GetStudentsPagedAsync_WhenUsersExist_ReturnsResponseBuiltFromFetchedPage()
    {
        const int requestedPageIndex = 0;
        const long totalCount = 25;
        List<User> fetchedUsers =
        [
            new() { Id = Guid.NewGuid(), UserName = "student_one", Role = UserRoles.Student },
            new() { Id = Guid.NewGuid(), UserName = "student_two", Role = UserRoles.Student }
        ];
        PagedUsersResponseDto builtResponse = new() { Items = [], PageIndex = 0, MaxPageIndex = 2 };

        userDirectoryDao
            .Setup(dao => dao.CountByRoleForTenantAsync(CallerTenantId, UserRoles.Student))
            .ReturnsAsync(totalCount);
        userDirectoryDao
            .Setup(dao => dao.GetByRoleForTenantPagedAsync(
                CallerTenantId, UserRoles.Student, 0, UserDirectoryService.UserListPageSize))
            .ReturnsAsync(fetchedUsers);
        userViewBuilder
            .Setup(builder => builder.BuildPagedUsersResponse(fetchedUsers, 0, 2))
            .Returns(builtResponse);

        PagedUsersResponseDto response = await sut.GetStudentsPagedAsync(requestedPageIndex);

        Assert.That(response, Is.SameAs(builtResponse));
    }

    [Test]
    public async Task GetStudentsPagedAsync_WhenTotalCountZero_ReturnsEmptyPageWithoutFetching()
    {
        PagedUsersResponseDto builtResponse = new() { Items = [], PageIndex = 0, MaxPageIndex = 0 };
        userDirectoryDao
            .Setup(dao => dao.CountByRoleForTenantAsync(CallerTenantId, UserRoles.Student))
            .ReturnsAsync(0L);
        userViewBuilder
            .Setup(builder => builder.BuildPagedUsersResponse(
                It.Is<List<User>>(list => list.Count == 0), 0, 0))
            .Returns(builtResponse);

        PagedUsersResponseDto response = await sut.GetStudentsPagedAsync(0);

        Assert.That(response, Is.SameAs(builtResponse));
        userDirectoryDao.Verify(
            dao => dao.GetByRoleForTenantPagedAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Test]
    public async Task GetTeachersPagedAsync_WhenUsersExist_ReturnsResponseBuiltFromFetchedPage()
    {
        const int requestedPageIndex = 1;
        const long totalCount = 12;
        List<User> fetchedUsers =
        [
            new() { Id = Guid.NewGuid(), UserName = "teacher_one", Role = UserRoles.Teacher }
        ];
        PagedUsersResponseDto builtResponse = new() { Items = [], PageIndex = 1, MaxPageIndex = 1 };

        userDirectoryDao
            .Setup(dao => dao.CountByRoleForTenantAsync(CallerTenantId, UserRoles.Teacher))
            .ReturnsAsync(totalCount);
        userDirectoryDao
            .Setup(dao => dao.GetByRoleForTenantPagedAsync(
                CallerTenantId, UserRoles.Teacher, UserDirectoryService.UserListPageSize, UserDirectoryService.UserListPageSize))
            .ReturnsAsync(fetchedUsers);
        userViewBuilder
            .Setup(builder => builder.BuildPagedUsersResponse(fetchedUsers, 1, 1))
            .Returns(builtResponse);

        PagedUsersResponseDto response = await sut.GetTeachersPagedAsync(requestedPageIndex);

        Assert.That(response, Is.SameAs(builtResponse));
    }
}

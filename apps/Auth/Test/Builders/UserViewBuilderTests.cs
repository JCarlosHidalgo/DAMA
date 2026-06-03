using Backend.Builders;
using Backend.Dtos.Users.Output;
using Backend.Entities.Users;
using Backend.Security;

namespace Test.Builders;

[TestFixture]
public class UserViewBuilderTests
{
    private UserViewBuilder sut = null!;

    [SetUp]
    public void SetUp() => sut = new UserViewBuilder();

    [Test]
    public void BuildUserListItem_MapsIdAndUserName()
    {
        User user = new()
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            UserName = "some_user",
            Role = UserRoles.Student
        };

        UserListItemDto listItem = sut.BuildUserListItem(user);

        Assert.Multiple(() =>
        {
            Assert.That(listItem.Id, Is.EqualTo(user.Id));
            Assert.That(listItem.Username, Is.EqualTo("some_user"));
        });
    }

    [Test]
    public void BuildPagedUsersResponse_MapsAllUsersAndPropagatesPaginationFields()
    {
        List<User> users =
        [
            new() { Id = Guid.NewGuid(), UserName = "first_user", Role = UserRoles.Student },
            new() { Id = Guid.NewGuid(), UserName = "second_user", Role = UserRoles.Student }
        ];

        PagedUsersResponseDto response = sut.BuildPagedUsersResponse(users, pageIndex: 2, maxPageIndex: 5);

        Assert.That(response.Items, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(response.Items[0].Id, Is.EqualTo(users[0].Id));
            Assert.That(response.Items[0].Username, Is.EqualTo("first_user"));
            Assert.That(response.Items[1].Id, Is.EqualTo(users[1].Id));
            Assert.That(response.Items[1].Username, Is.EqualTo("second_user"));
            Assert.That(response.PageIndex, Is.EqualTo(2));
            Assert.That(response.MaxPageIndex, Is.EqualTo(5));
        });
    }

    [Test]
    public void BuildPagedUsersResponse_WithEmptyUserList_ReturnsEmptyItems()
    {
        PagedUsersResponseDto response = sut.BuildPagedUsersResponse([], pageIndex: 0, maxPageIndex: 0);

        Assert.Multiple(() =>
        {
            Assert.That(response.Items, Is.Empty);
            Assert.That(response.PageIndex, Is.EqualTo(0));
            Assert.That(response.MaxPageIndex, Is.EqualTo(0));
        });
    }
}

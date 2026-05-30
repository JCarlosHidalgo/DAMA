using Backend.Dtos.Users.Output;
using Backend.Entities.Users;

namespace Backend.Builders;

public class UserViewBuilder : IUserViewBuilder
{
    public UserListItemDto BuildUserListItem(User user)
    {
        return new UserListItemDto
        {
            Id = user.Id,
            Username = user.UserName
        };
    }

    public PagedUsersResponseDto BuildPagedUsersResponse(List<User> users, int pageIndex, int maxPageIndex)
    {
        return new PagedUsersResponseDto
        {
            Items = users.Select(BuildUserListItem).ToList(),
            PageIndex = pageIndex,
            MaxPageIndex = maxPageIndex
        };
    }
}

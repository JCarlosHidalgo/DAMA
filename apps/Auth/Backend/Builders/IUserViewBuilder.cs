using Backend.Dtos.Users.Output;
using Backend.Entities.Users;

namespace Backend.Builders;

public interface IUserViewBuilder
{
    UserListItemDto BuildUserListItem(User user);

    PagedUsersResponseDto BuildPagedUsersResponse(List<User> users, int pageIndex, int maxPageIndex);
}

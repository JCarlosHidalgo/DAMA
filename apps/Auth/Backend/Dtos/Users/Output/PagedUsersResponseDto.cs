namespace Backend.Dtos.Users.Output;

public class PagedUsersResponseDto
{
    public List<UserListItemDto> Items { get; set; } = new();

    public int PageIndex { get; set; }

    public int MaxPageIndex { get; set; }
}

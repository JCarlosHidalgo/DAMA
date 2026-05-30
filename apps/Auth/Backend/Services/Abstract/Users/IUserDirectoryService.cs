using Backend.Dtos.Users.Output;
using Backend.Results.Users;

namespace Backend.Services.Abstract.Users;

public interface IUserDirectoryService
{
    Task<PagedUsersResponseDto> GetStudentsPagedAsync(int pageIndex);

    Task<PagedUsersResponseDto> GetTeachersPagedAsync(int pageIndex);

    Task<DeleteUserOutcome> DeleteUserAsync(Guid targetUserId);

    Task<RenameUserOutcome> RenameUserAsync(Guid targetUserId, string newUserName);

    Task<UserListItemDto?> FindStudentByExactNameAsync(string userName);
}

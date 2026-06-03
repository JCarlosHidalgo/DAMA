using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Output;
using Backend.Entities.Users;
using Backend.Pagination;
using Backend.Results.Users;
using Backend.Security;
using Backend.Services.Abstract.Users;

namespace Backend.Services.Concrete.Users;

public class UserDirectoryService : IUserDirectoryService
{
    public const int UserListPageSize = 10;

    private readonly IUserDirectoryDao _userDao;
    private readonly IClaimContext _claimContext;
    private readonly IUserViewBuilder _viewBuilder;

    public UserDirectoryService(IUserDirectoryDao userDao,
                                IClaimContext claimContext,
                                IUserViewBuilder viewBuilder)
    {
        _userDao = userDao;
        _claimContext = claimContext;
        _viewBuilder = viewBuilder;
    }

    public Task<PagedUsersResponseDto> GetStudentsPagedAsync(int pageIndex) =>
        GetUsersPagedByRoleAsync(pageIndex, UserRole.Student);

    public Task<PagedUsersResponseDto> GetTeachersPagedAsync(int pageIndex) =>
        GetUsersPagedByRoleAsync(pageIndex, UserRole.Teacher);

    private async Task<PagedUsersResponseDto> GetUsersPagedByRoleAsync(int requestedPageIndex, UserRole role)
    {
        Guid tenantId = _claimContext.TenantId;

        long totalCount = await _userDao.CountByRoleForTenantAsync(tenantId, role.Value);

        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount, requestedPageIndex, UserListPageSize);

        List<User> users = totalCount <= 0
            ? new List<User>()
            : await _userDao.GetByRoleForTenantPagedAsync(tenantId, role.Value, offset, UserListPageSize);

        return _viewBuilder.BuildPagedUsersResponse(users, effectivePageIndex, maxPageIndex);
    }

    public async Task<DeleteUserOutcome> DeleteUserAsync(Guid targetUserId)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid callerId = _claimContext.UserId;

        if (targetUserId == callerId)
        {
            return new DeleteUserOutcome.SelfDeleteForbidden();
        }

        User? target = await _userDao.GetByIdForTenantAsync(targetUserId, tenantId);
        if (target is null)
        {
            return new DeleteUserOutcome.NotFound();
        }

        if (target.Role == UserRoles.Client)
        {
            return new DeleteUserOutcome.ClientDeleteForbidden();
        }

        int affected = await _userDao.SoftDeleteForTenantAsync(targetUserId, tenantId);
        return affected > 0 ? new DeleteUserOutcome.Deleted() : new DeleteUserOutcome.NotFound();
    }

    public async Task<RenameUserOutcome> RenameUserAsync(Guid targetUserId, string newUserName)
    {
        Guid tenantId = _claimContext.TenantId;

        User? target = await _userDao.GetByIdForTenantAsync(targetUserId, tenantId);
        if (target is null)
        {
            return new RenameUserOutcome.NotFound();
        }

        int result = await _userDao.TryUpdateUserNameForTenantAsync(targetUserId, tenantId, newUserName);
        if (result == -1)
        {
            return new RenameUserOutcome.DuplicateName();
        }

        if (result == 0)
        {
            return new RenameUserOutcome.NotFound();
        }

        return new RenameUserOutcome.Renamed();
    }

    public async Task<UserListItemDto?> FindStudentByExactNameAsync(string userName)
    {
        Guid tenantId = _claimContext.TenantId;

        User? user = await _userDao.GetStudentByExactNameForTenantAsync(tenantId, userName);
        if (user is null)
        {
            return null;
        }

        return _viewBuilder.BuildUserListItem(user);
    }
}

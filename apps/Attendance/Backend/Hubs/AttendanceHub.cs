using Backend.Claims;
using Backend.Entities.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

[Authorize(Roles = UserRoles.ClientOrTeacher)]
public class AttendanceHub : Hub
{
    private readonly IHubClaimContext _hubClaimContext;

    public AttendanceHub(IHubClaimContext hubClaimContext)
    {
        _hubClaimContext = hubClaimContext;
    }

    public Task JoinScheduledClass(Guid classId, DateOnly classDate)
    {
        string groupName = ScheduledGroup(ReadTenantId(), classId, classDate);
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public Task LeaveScheduledClass(Guid classId, DateOnly classDate)
    {
        string groupName = ScheduledGroup(ReadTenantId(), classId, classDate);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public Task JoinUniqueClass(Guid classId)
    {
        string groupName = UniqueGroup(ReadTenantId(), classId);
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public Task LeaveUniqueClass(Guid classId)
    {
        string groupName = UniqueGroup(ReadTenantId(), classId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public static string ScheduledGroup(Guid tenantId, Guid classId, DateOnly classDate)
    {
        return $"scheduled:{tenantId}:{classId}:{classDate:yyyy-MM-dd}";
    }

    public static string UniqueGroup(Guid tenantId, Guid classId)
    {
        return $"unique:{tenantId}:{classId}";
    }

    private Guid ReadTenantId()
    {
        try
        {
            return _hubClaimContext.GetTenantId(Context.User);
        }
        catch (MissingClaimException)
        {
            throw new HubException("Missing tenant claim.");
        }
    }
}

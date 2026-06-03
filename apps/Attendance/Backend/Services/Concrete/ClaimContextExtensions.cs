using Backend.Claims;
using Backend.Security;

namespace Backend.Services.Concrete;

public static class ClaimContextExtensions
{
    public static bool IsStudentAccessingOtherStudent(this IClaimContext claimContext, Guid requestedStudentId)
    {
        if (claimContext.Role != UserRoles.Student)
        {
            return false;
        }
        return claimContext.UserId != requestedStudentId;
    }
}

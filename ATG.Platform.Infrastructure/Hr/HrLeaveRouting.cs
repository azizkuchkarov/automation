using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrLeaveRouting
{
    public const string HoHrDepartmentCode = "HO-HR";
    public const string BmgmcHrDepartmentCode = "BMGMC-HR";

    public static string ResolveHrDepartmentCode(Organization organization)
    {
        if (organization.OrgType == OrgType.HeadOffice || organization.Code.Equals("HO", StringComparison.OrdinalIgnoreCase))
            return HoHrDepartmentCode;

        return BmgmcHrDepartmentCode;
    }

    public static HrLeaveTrack ResolveTrack(User user) =>
        user.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager
            ? HrLeaveTrack.DepartmentHead
            : HrLeaveTrack.Specialist;
}

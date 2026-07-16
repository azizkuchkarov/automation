namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripWorkflowResolver
{
    private static readonly HashSet<string> DirectDepartmentCodes =
    [
        "HO-HR", "HO-SEC", "HO-AC", "HO-FINPLAN", "HO-ACCT", "HO-ENGCON", "HO-NEWPRJ",
        "HO-ITDIG", "HO-MKT", "HO-CPROC", "HO-DCPR", "HO-QHSE", "HO-GASM", "HO-LEGAL",
        "HO-ADM", "HO-EXEC",
    ];

    public static string? ResolveWorkflowDepartmentCode(string? departmentCode)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
            return null;

        if (DirectDepartmentCodes.Contains(departmentCode))
            return departmentCode;

        if (departmentCode.StartsWith("HO-CPROC", StringComparison.Ordinal))
            return "HO-CPROC";
        if (departmentCode.StartsWith("HO-MKT", StringComparison.Ordinal))
            return "HO-MKT";
        if (departmentCode.StartsWith("HO-DCPR", StringComparison.Ordinal))
            return "HO-DCPR";
        if (departmentCode.StartsWith("HO-ADM", StringComparison.Ordinal))
            return "HO-ADM";

        return null;
    }

    public static string? ResolveCprocSectionManagerEmail(string? departmentCode) => departmentCode switch
    {
        "HO-CPROC-CADM" => "zhaomao@atg.uz",
        "HO-CPROC-DOM" => "r.avezov@atg.uz",
        "HO-CPROC-INT" => "i.raimjanov@atg.uz",
        _ => null,
    };
}

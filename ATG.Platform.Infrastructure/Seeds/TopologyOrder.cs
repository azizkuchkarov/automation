namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>Canonical org-chart display order — matches master data / spreadsheet sequence.</summary>
public static class TopologyOrder
{
    public static readonly string[] OrganizationCodes =
    [
        HoMasterData.OrganizationCode,
        BmgmcMasterData.OrganizationCode,
        Wkc1Ucs1MasterData.OrganizationCode,
        Wkc2GcsMasterData.OrganizationCode,
        Wkc3MasterData.OrganizationCode,
        Ucs3MasterData.OrganizationCode,
        MsUkmsMasterData.OrganizationCode,
    ];

    private static readonly Dictionary<string, int> DepartmentOrder = BuildDepartmentOrder();

    private static Dictionary<string, int> BuildDepartmentOrder()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        void AddRange(IEnumerable<HoDepartment> depts)
        {
            foreach (var d in depts)
            {
                if (!d.Active || map.ContainsKey(d.Code)) continue;
                map[d.Code] = index++;
            }
        }

        AddRange(HoMasterData.Departments);
        AddRange(BmgmcMasterData.Departments);
        AddRange(Wkc1Ucs1MasterData.Departments);
        AddRange(Wkc2GcsMasterData.Departments);
        AddRange(Wkc3MasterData.Departments);
        AddRange(Ucs3MasterData.Departments);
        AddRange(MsUkmsMasterData.Departments);

        return map;
    }

    public static int GetOrganizationOrder(string code)
    {
        var i = Array.FindIndex(OrganizationCodes, c => c.Equals(code, StringComparison.OrdinalIgnoreCase));
        return i >= 0 ? i : 900 + Math.Abs(code.GetHashCode(StringComparison.OrdinalIgnoreCase) % 100);
    }

    public static int GetDepartmentOrder(string code) =>
        DepartmentOrder.TryGetValue(code, out var i) ? i : 9000;
}

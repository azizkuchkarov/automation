using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATG.Platform.Infrastructure.Seeds;

public static class ItAutomationSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var cat in Enum.GetValues<ItAssetCategory>())
        {
            if (!await db.ItAutomationRoleAssignments.AnyAsync(r => r.Category == cat))
            {
                db.ItAutomationRoleAssignments.Add(new ItAutomationRoleAssignment
                {
                    Category = cat,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
        }
        await db.SaveChangesAsync();

        if (await db.ItAssets.AnyAsync())
            return;

        var items = Build2026Workplan();
        db.ItAssets.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static List<ItAsset> Build2026Workplan()
    {
        var list = new List<ItAsset>();

        void Add(
            ItAssetCategory cat,
            string nameEn,
            string? nameRu,
            string? qty,
            string? term,
            string? contractNo,
            DateTime? contractDate,
            decimal? cost,
            string status,
            string? note = null,
            DateTime? expiresOverride = null)
        {
            var expires = expiresOverride
                ?? (contractDate.HasValue && string.Equals(term, "Annual", StringComparison.OrdinalIgnoreCase)
                    ? contractDate.Value.AddYears(1)
                    : null);

            list.Add(new ItAsset
            {
                Id = Guid.NewGuid(),
                Category = cat,
                NameEn = nameEn,
                NameRu = nameRu ?? nameEn,
                Quantity = qty,
                Term = term,
                ContractNumber = contractNo,
                ContractDate = contractDate,
                Cost = cost,
                ExpiresAt = expires,
                StartsAt = contractDate,
                Status = ParseStatus(status),
                Note = note,
                PlanYear = 2026,
                Currency = cost.HasValue ? "UZS" : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        // —— Licenses ——
        Add(ItAssetCategory.License, "Microsoft Visio", "Microsoft Visio", "10", "Annual", "4800728", new DateTime(2026, 6, 12), 18_999_000m, "Done");
        Add(ItAssetCategory.License, "MS 365", "MS 365", "150/300", "Annual", "6764149.1.1", new DateTime(2026, 3, 5), 989_000_000m, "Done");
        Add(ItAssetCategory.License, "Adobe Creative Cloud", "Adobe Creative Cloud", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.License, "Adobe Acrobat PRO", "Adobe Acrobat PRO", "95", "Annual", "6734842.1.1", new DateTime(2026, 3, 3), 246_528_800m, "Done");
        Add(ItAssetCategory.License, "AutoCAD", "AutoCAD", "8", "Annual", "7897544.1.1", new DateTime(2026, 7, 7), 188_835_368.96m, "Done");
        Add(ItAssetCategory.License, "Veeam Data Platform Foundation Enterprise", "Veeam Data Platform Foundation Enterprise", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.License, "Kaspersky Total Security and EDR", "Kaspersky Total Security и EDR", "300", "Annual", "5932248.1.1", new DateTime(2025, 4, 17), 519_000_000m, "Done");
        Add(ItAssetCategory.License, "Norma Hamkor", "Norma Hamkor", null, "Annual", "260248Б", new DateTime(2026, 4, 30), 121_000_000m, "Done");
        Add(ItAssetCategory.License, "VMware Tashkent", "VMware Tashkent", "320", "Annual", "6818945.1.1", new DateTime(2026, 3, 13), 437_729_600m, "Done");
        Add(ItAssetCategory.License, "VMware Bukhara", "VMware Bukhara", "160", "Annual", "6818935.1.1", new DateTime(2026, 3, 13), 218_864_800m, "Done");
        Add(ItAssetCategory.License, "SSL certificates", "SSL-сертификаты", "1", "Annual", null, null, null, "InProcess");
        Add(ItAssetCategory.License, "Cisco NGFW", "Cisco NGFW", "1", "Annual", null, null, null, "InProcess");
        Add(ItAssetCategory.License, "NetApp support", "Поддержка NetApp", "1", "Annual", "41245542", new DateTime(2026, 2, 13), 240_000_000m, "Done");
        Add(ItAssetCategory.License, "1C", "1С", "1", "Annual", "4380165", new DateTime(2026, 4, 21), 6_000_000m, "Done", "total amount 73561000 with licence");
        Add(ItAssetCategory.License, "Warranty Cisco SAN 9148S", "Гарантия Cisco SAN 9148S", "2", "27 month", "4255350", new DateTime(2026, 3, 18), 105_000_000m, "Done", null, new DateTime(2026, 3, 18).AddMonths(27));

        // —— Services ——
        Add(ItAssetCategory.Service, "Automatic gas fire-fighting system maintenance", "ТО системы газового пожаротушения", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.Service, "Server and network infrastructure support", "ТО серверной и сетевой инфраструктуры", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.Service, "VoIP services of Uztelecom", "IP-телефония Узбектелеком", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.Service, "Internet services of Uztelecom", "Интернет Узбектелеком", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.Service, "IP TV service", "IP TV", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.Service, "Centralized print management system", "Централизованное управление печатью", null, null, null, null, null, "Active");
        Add(ItAssetCategory.Service, "SharePoint (portal) support", "Сопровождение SharePoint (портал)", null, null, null, null, null, "Active");
        Add(ItAssetCategory.Service, "Servercore", "Servercore", null, null, null, null, null, "Active");
        Add(ItAssetCategory.Service, "Consulting ISO 27001", "Консалтинг ISO 27001", null, null, "ATG-CD-S-2026-I-0006 1", new DateTime(2026, 2, 26), 496_000_000m, "Done", "End of year", new DateTime(2026, 12, 31));
        Add(ItAssetCategory.Service, "Alfa Bukhara", "Alfa Bukhara", null, "Annual", "5-18 / ATG-AD-S-2021-0037", new DateTime(2021, 5, 18), 100_000_000m, "Done", "Contract signed for 100,000,000 soums in 2021");
        Add(ItAssetCategory.Service, "Inmarsat (Tarlan Telekom)", "Inmarsat (Tarlan Telekom)", null, "Annual", "ATG-CD-S-2026-I-0020", new DateTime(2026, 5, 14), 95_000_000m, "Done", "Estimated contract amount: 7910");
        Add(ItAssetCategory.Service, "Certification ISO 27001", "Сертификация ISO 27001", null, null, null, null, null, "Active");

        // —— Mobile ——
        Add(ItAssetCategory.MobileService, "Ucell Tashkent", "Ucell Ташкент", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.MobileService, "Ucell Bukhara", "Ucell Бухара", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.MobileService, "Unitel", "Unitel", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.MobileService, "Perfectum", "Perfectum", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.MobileService, "Uzmobile", "Uzmobile", null, "Annual", null, null, null, "Active");

        // —— Government ——
        Add(ItAssetCategory.GovernmentService, "VSAT System License", "Лицензия VSAT", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.GovernmentService, "Permission for portable VHF radio (Bukhara, Kashkadarya, Navoiy)", "Разрешение на переносные УКВ радиостанции", "60", "Annual", null, null, null, "Done");
        Add(ItAssetCategory.GovernmentService, "E-IMZO", "E-IMZO", "1", "Annual", "333/2026-3", new DateTime(2026, 5, 4), 4_459_177m, "Done");
        Add(ItAssetCategory.GovernmentService, "CEMC", "СЭМС", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.GovernmentService, "E-xat", "E-xat", null, "Annual", null, null, null, "Active");
        Add(ItAssetCategory.GovernmentService, "Cable Duct System", "Кабельная канализация", "1", "Annual", "230-20-8", new DateTime(2020, 1, 4), 103_815_600m, "Done", "According to the 5 supplementary agreement from 01.05.2026");
        Add(ItAssetCategory.GovernmentService, "Time Service Uzbektelecom Bukhara", "Служба времени Узбектелеком Бухара", "1", "Annual", "1831", new DateTime(2026, 2, 27), 32_136_000m, "Done");
        Add(ItAssetCategory.GovernmentService, "Uzsanjapsputnik", "Узсвязьспутник", null, null, null, null, null, "Active");
        Add(ItAssetCategory.GovernmentService, "License of international communication network", "Лицензия международной сети связи", "1", "Annual", null, null, 206_000_000m, "Done", "500 БРВ");
        Add(ItAssetCategory.GovernmentService, "License of local communication network", "Лицензия местной сети связи", "1", "Annual", null, null, 12_360_000m, "Done", "30 БРВ");
        Add(ItAssetCategory.GovernmentService, "License of Data", "Лицензия на передачу данных", "1", "Annual", null, null, 2_472_000m, "Done", "6 БРВ");

        // —— Equipment ——
        Add(ItAssetCategory.Equipment, "Patch cords", "Патч-корды", null, null, "7884204.1.1", new DateTime(2026, 7, 6), 4_137_500m, "Done", "Lider Team");
        Add(ItAssetCategory.Equipment, "Cartridges", "Картриджи", null, null, "ATG-CD-M-2026-I-006", new DateTime(2026, 6, 17), 165_009_600m, "Done", "First Elektroniks");

        return list;
    }

    private static ItAssetStatus ParseStatus(string status) => status.ToLowerInvariant() switch
    {
        "done" => ItAssetStatus.Done,
        "inprocess" or "in process" => ItAssetStatus.InProcess,
        "expired" => ItAssetStatus.Expired,
        "suspended" => ItAssetStatus.Suspended,
        "cancelled" => ItAssetStatus.Cancelled,
        _ => ItAssetStatus.Active,
    };
}

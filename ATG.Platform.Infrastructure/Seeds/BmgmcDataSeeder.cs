using ATG.Platform.Domain.Entities;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ATG.Platform.Infrastructure.Seeds;

public static class BmgmcDataSeeder
{
    private static readonly Dictionary<string, string> LocalDevPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user1@atg.uz"] = "12345",
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        var bmgmc = await db.Organizations.FirstOrDefaultAsync(o => o.Code == BmgmcMasterData.OrganizationCode);
        if (bmgmc is null) return;

        logger.LogInformation("Syncing BMGMC structure...");

        var deptIds = await DepartmentSeederHelper.SyncDepartmentsAsync(
            db, bmgmc.Id, BmgmcMasterData.Departments, BmgmcMasterData.LegacyDepartmentCodes);

        var positions = await db.Positions.ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);
        var staffCreated = 0;
        var staffUpdated = 0;

        foreach (var member in BmgmcMasterData.Staff)
        {
            if (!deptIds.TryGetValue(member.DepartmentCode, out var departmentId))
            {
                logger.LogWarning("Department {Code} not found for {Email}", member.DepartmentCode, member.Email);
                continue;
            }

            if (!positions.TryGetValue(member.PositionCode, out var positionId))
            {
                logger.LogWarning("Position {Code} not found for {Email}", member.PositionCode, member.Email);
                continue;
            }

            var phone = FormatPhone(member.Phone, member.InternalExt);
            var email = member.Email.ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
            {
                user = new User
                {
                    EmployeeId = member.EmployeeId,
                    FirstName = member.FirstNameRu,
                    LastName = member.LastNameRu,
                    MiddleName = member.MiddleNameRu,
                    FirstNameEn = member.FirstNameEn,
                    LastNameEn = member.LastNameEn,
                    MiddleNameEn = member.MiddleNameEn,
                    JobTitleRu = member.JobTitleRu,
                    JobTitleEn = member.JobTitleEn,
                    Email = email,
                    Phone = phone,
                    PasswordHash = string.Empty,
                    OrganizationId = bmgmc.Id,
                    DepartmentId = departmentId,
                    PositionId = positionId,
                    Role = member.Role,
                    Language = "ru"
                };
                db.Users.Add(user);
                staffCreated++;
            }
            else
            {
                user.EmployeeId = member.EmployeeId;
                user.FirstName = member.FirstNameRu;
                user.LastName = member.LastNameRu;
                user.MiddleName = member.MiddleNameRu;
                user.FirstNameEn = member.FirstNameEn;
                user.LastNameEn = member.LastNameEn;
                user.MiddleNameEn = member.MiddleNameEn;
                user.JobTitleRu = member.JobTitleRu;
                user.JobTitleEn = member.JobTitleEn;
                user.Phone = phone;
                user.OrganizationId = bmgmc.Id;
                user.DepartmentId = departmentId;
                user.PositionId = positionId;
                user.Role = member.Role;
                user.UpdatedAt = DateTime.UtcNow;
                staffUpdated++;
            }

            if (LocalDevPasswords.TryGetValue(email, out var devPassword))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(devPassword, 12);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("BMGMC sync complete: {Created} users created, {Updated} updated", staffCreated, staffUpdated);
    }

    private static string? FormatPhone(string? mobile, string? ext)
    {
        if (string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(ext)) return null;
        if (string.IsNullOrWhiteSpace(ext)) return mobile;
        if (string.IsNullOrWhiteSpace(mobile)) return $"ext. {ext}";
        return $"{mobile} (ext. {ext})";
    }
}

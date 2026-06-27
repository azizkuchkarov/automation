using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ATG.Platform.Infrastructure.Seeds;

public static class TaskDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (await db.WorkTasks.AnyAsync()) return;

        var itDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "HO-ITDIG");
        var hrDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "HO-HR");
        var dcsDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "HO-DCPR");
        if (itDept is null) return;

        var staff = await db.Users
            .Where(u => u.DepartmentId == itDept.Id && u.IsActive)
            .OrderBy(u => u.EmployeeId)
            .ToListAsync();

        if (staff.Count < 2) return;

        var manager = staff.FirstOrDefault(u => u.Role == UserRole.HONachalnik) ?? staff[0];
        var engineers = staff.Where(u => u.Id != manager.Id).Take(5).ToList();

        var samples = new (string Title, WorkTaskStatus Status, TaskSource Source, int DaysAgo, string? DeptCode)[]
        {
            ("Migrate legacy file server to cloud storage", WorkTaskStatus.InProgress, TaskSource.Manual, 5, "HO-ITDIG"),
            ("Deploy MFA for all HO accounts", WorkTaskStatus.New, TaskSource.Manual, 1, "HO-ITDIG"),
            ("Update network diagram documentation", WorkTaskStatus.Done, TaskSource.Manual, 12, "HO-ITDIG"),
            ("Approve incoming contract — vendor NDA", WorkTaskStatus.New, TaskSource.DCS, 2, "HO-DCPR"),
            ("Register Q2 compliance document package", WorkTaskStatus.InProgress, TaskSource.DCS, 4, "HO-DCPR"),
            ("Archive board meeting minutes", WorkTaskStatus.Done, TaskSource.DCS, 10, "HO-DCPR"),
            ("Process annual leave request batch", WorkTaskStatus.New, TaskSource.HR, 1, "HO-HR"),
            ("Update employee handbook translation", WorkTaskStatus.InProgress, TaskSource.HR, 3, "HO-HR"),
            ("Onboard 3 new engineers — paperwork", WorkTaskStatus.Done, TaskSource.HR, 7, "HO-HR"),
            ("Patch critical CVE on domain controllers", WorkTaskStatus.InProgress, TaskSource.Manual, 3, "HO-ITDIG"),
        };

        var deptMap = new Dictionary<string, Department> { [itDept.Code] = itDept };
        if (hrDept is not null) deptMap[hrDept.Code] = hrDept;
        if (dcsDept is not null) deptMap[dcsDept.Code] = dcsDept;

        var hrUsers = hrDept is not null
            ? await db.Users.Where(u => u.DepartmentId == hrDept.Id && u.IsActive).Take(3).ToListAsync()
            : [];
        var dcsUsers = dcsDept is not null
            ? await db.Users.Where(u => u.DepartmentId == dcsDept.Id && u.IsActive).Take(3).ToListAsync()
            : [];

        for (var i = 0; i < samples.Length; i++)
        {
            var (title, status, source, daysAgo, deptCode) = samples[i];
            var dept = deptMap.GetValueOrDefault(deptCode ?? itDept.Code) ?? itDept;
            var assignee = source switch
            {
                TaskSource.HR when hrUsers.Count > 0 => hrUsers[i % hrUsers.Count],
                TaskSource.DCS when dcsUsers.Count > 0 => dcsUsers[i % dcsUsers.Count],
                _ => engineers[i % engineers.Count],
            };
            var created = DateTime.UtcNow.AddDays(-daysAgo);

            db.WorkTasks.Add(new WorkTask
            {
                Id = Guid.NewGuid(),
                Number = $"TSK-{(i + 1):D5}",
                Title = title,
                Description = $"Sample {source} task",
                Status = status,
                Priority = i % 3 == 0 ? TaskPriority.High : TaskPriority.Medium,
                Source = source,
                AssigneeId = assignee.Id,
                CreatedById = manager.Id,
                OrganizationId = assignee.OrganizationId,
                DepartmentId = dept.Id,
                CreatedAt = created,
                UpdatedAt = created.AddDays(1),
                StartedAt = status is WorkTaskStatus.InProgress or WorkTaskStatus.Done ? created.AddHours(4) : null,
                CompletedAt = status == WorkTaskStatus.Done ? created.AddDays(2) : null,
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} sample work tasks (Manual, DCS, HR)", samples.Length);
    }
}

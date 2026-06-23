using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ATG.Platform.Infrastructure.Seeds;

public static class DatabaseSeeder
{
  public static async Task SeedAsync(IServiceProvider services)
  {
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Data.AppDbContext>>();

    await db.Database.MigrateAsync();

    if (await db.Organizations.AnyAsync()) return;

    logger.LogInformation("Seeding database...");

    var ho = new Organization { Name = "Tashkent Head Office", Code = "HO", OrgType = OrgType.HeadOffice };
    var bmgmc = new Organization { Name = "BMGMC", Code = "BMGMC", OrgType = OrgType.BMGMC, Parent = ho };
    var stations = new[]
    {
      new Organization { Name = "WKC1", Code = "WKC1", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "WKC2", Code = "WKC2", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "UCS1", Code = "UCS1", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "GCS", Code = "GCS", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "WKC3", Code = "WKC3", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "UCS3", Code = "UCS3", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "MS", Code = "MS", OrgType = OrgType.Station, Parent = bmgmc },
      new Organization { Name = "UKMS", Code = "UKMS", OrgType = OrgType.Station, Parent = bmgmc }
    };

    db.Organizations.Add(ho);
    db.Organizations.Add(bmgmc);
    db.Organizations.AddRange(stations);

    var positions = new[]
    {
      new Position { Name = "Engineer", Code = "ENGINEER" },
      new Position { Name = "Manager", Code = "MANAGER" },
      new Position { Name = "Nachalnik otdeli", Code = "NACHALNIK" },
      new Position { Name = "Top Manager", Code = "TOP_MANAGER" },
      new Position { Name = "Technician", Code = "TECHNICIAN" },
      new Position { Name = "Specialist", Code = "SPECIALIST" },
      new Position { Name = "Administrator", Code = "ADMIN" },
      new Position { Name = "Operator", Code = "OPERATOR" }
    };
    db.Positions.AddRange(positions);

    var deptNames = new[] { ("Operations", "OPS"), ("Engineering", "ENG"), ("Administration", "ADM"), ("Safety", "SAF"), ("IT", "IT") };
    var allOrgs = new List<Organization> { ho, bmgmc };
    allOrgs.AddRange(stations);

    foreach (var org in allOrgs)
    {
      foreach (var (name, code) in deptNames)
      {
        db.Departments.Add(new Department
        {
          Organization = org,
          Name = name,
          Code = $"{org.Code}-{code}"
        });
      }
    }

    await db.SaveChangesAsync();

    var hoDept = await db.Departments.FirstAsync(d => d.Code == "HO-OPS");
    db.Users.Add(new User
    {
      EmployeeId = "ATG-001",
      FirstName = "System",
      LastName = "Administrator",
      Email = "admin@atg.uz",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2024!", 12),
      Organization = ho,
      Department = hoDept,
      Position = positions[6],
      Role = UserRole.SuperAdmin,
      Language = "ru"
    });

    await db.SaveChangesAsync();
    logger.LogInformation("Database seeded successfully");
  }
}

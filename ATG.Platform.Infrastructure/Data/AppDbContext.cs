using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(20);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.OrgType).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(30);
            e.HasOne(x => x.Organization).WithMany(x => x.Departments).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<Position>(e =>
        {
            e.ToTable("positions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(30);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeId).HasMaxLength(20);
            e.HasIndex(x => x.EmployeeId).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.MiddleName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Language).HasMaxLength(5);
            e.HasOne(x => x.Organization).WithMany(x => x.Users).HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Department).WithMany(x => x.Users).HasForeignKey(x => x.DepartmentId);
            e.HasOne(x => x.Position).WithMany(x => x.Users).HasForeignKey(x => x.PositionId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
    }
}

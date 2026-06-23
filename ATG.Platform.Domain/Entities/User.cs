using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AvatarUrl { get; set; }
    public string Language { get; set; } = "ru";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Department? Department { get; set; }
    public Position? Position { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
}

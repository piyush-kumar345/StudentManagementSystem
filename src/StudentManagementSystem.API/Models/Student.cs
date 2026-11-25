namespace StudentManagementSystem.API.Models;

/// <summary>
/// Represents a student record stored in SQL Server.
/// </summary>
public class Student
{
    public int Id { get; set; }

    public string EnrollmentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public int EnrollmentYear { get; set; }

    public string? Address { get; set; }

    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

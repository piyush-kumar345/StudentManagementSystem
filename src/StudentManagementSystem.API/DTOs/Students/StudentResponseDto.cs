using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.DTOs.Students;

/// <summary>
/// Response payload representing a student record.
/// </summary>
public class StudentResponseDto
{
    public int Id { get; set; }

    public string EnrollmentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public int EnrollmentYear { get; set; }

    public string? Address { get; set; }

    public StudentStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

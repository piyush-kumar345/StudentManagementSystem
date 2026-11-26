using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.DTOs.Dashboard;

/// <summary>
/// Lightweight summary of a recently registered student for the dashboard feed.
/// </summary>
public class RecentStudentDto
{
    public int Id { get; set; }

    public string EnrollmentNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public int EnrollmentYear { get; set; }

    public StudentStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

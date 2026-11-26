namespace StudentManagementSystem.API.DTOs.Dashboard;

/// <summary>
/// Represents the number of students enrolled in a specific course.
/// </summary>
public class CourseCountDto
{
    public string Course { get; set; } = string.Empty;

    public int StudentCount { get; set; }
}

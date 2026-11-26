namespace StudentManagementSystem.API.DTOs.Dashboard;

/// <summary>
/// Represents student registration count for a calendar month in the current year.
/// </summary>
public class MonthlyEnrollmentDto
{
    public int Month { get; set; }

    public string MonthName { get; set; } = string.Empty;

    public int Count { get; set; }
}

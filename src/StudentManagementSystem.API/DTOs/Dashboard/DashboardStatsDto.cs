namespace StudentManagementSystem.API.DTOs.Dashboard;

/// <summary>
/// Aggregated student statistics returned for the admin dashboard.
/// </summary>
public class DashboardStatsDto
{
    public int TotalStudents { get; set; }

    public int ActiveStudents { get; set; }

    public int InactiveStudents { get; set; }

    public int GraduatedStudents { get; set; }

    public int CurrentYear { get; set; }

    public int EnrolledThisYear { get; set; }

    public int EnrolledLastYear { get; set; }

    public IReadOnlyList<CourseCountDto> CourseDistribution { get; set; } = [];

    public IReadOnlyList<MonthlyEnrollmentDto> MonthlyEnrollments { get; set; } = [];

    public IReadOnlyList<RecentStudentDto> RecentStudents { get; set; } = [];
}

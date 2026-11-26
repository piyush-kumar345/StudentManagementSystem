using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.DTOs.Dashboard;
using StudentManagementSystem.API.Interfaces;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Services;

/// <summary>
/// Computes dashboard statistics using database-side EF Core aggregations and read-only projections.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int RecentStudentsLimit = 5;

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DashboardService(AppDbContext dbContext, TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var currentYear = _timeProvider.GetUtcNow().UtcDateTime.Year;
        var lastYear = currentYear - 1;
        var startOfYear = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextYear = startOfYear.AddYears(1);

        var statusCounts = await _dbContext.Students
            .AsNoTracking()
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var activeCount = statusCounts.FirstOrDefault(x => x.Status == StudentStatus.Active)?.Count ?? 0;
        var inactiveCount = statusCounts.FirstOrDefault(x => x.Status == StudentStatus.Inactive)?.Count ?? 0;
        var graduatedCount = statusCounts.FirstOrDefault(x => x.Status == StudentStatus.Graduated)?.Count ?? 0;
        var totalStudents = activeCount + inactiveCount + graduatedCount;

        var enrolledThisYear = await _dbContext.Students
            .AsNoTracking()
            .CountAsync(s => s.EnrollmentYear == currentYear, cancellationToken);

        var enrolledLastYear = await _dbContext.Students
            .AsNoTracking()
            .CountAsync(s => s.EnrollmentYear == lastYear, cancellationToken);

        var courseDistribution = await _dbContext.Students
            .AsNoTracking()
            .GroupBy(s => s.Course)
            .Select(g => new CourseCountDto
            {
                Course = g.Key,
                StudentCount = g.Count()
            })
            .OrderByDescending(c => c.StudentCount)
            .ThenBy(c => c.Course)
            .ToListAsync(cancellationToken);

        var monthlyRows = await _dbContext.Students
            .AsNoTracking()
            .Where(s => s.CreatedAtUtc >= startOfYear && s.CreatedAtUtc < startOfNextYear)
            .GroupBy(s => s.CreatedAtUtc.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var monthlyLookup = monthlyRows.ToDictionary(x => x.Month, x => x.Count);
        var monthlyEnrollments = Enumerable.Range(1, 12)
            .Select(month => new MonthlyEnrollmentDto
            {
                Month = month,
                MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                Count = monthlyLookup.GetValueOrDefault(month, 0)
            })
            .ToList();

        var recentStudents = await _dbContext.Students
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Take(RecentStudentsLimit)
            .Select(s => new RecentStudentDto
            {
                Id = s.Id,
                EnrollmentNumber = s.EnrollmentNumber,
                FullName = s.FirstName + " " + s.LastName,
                Email = s.Email,
                Course = s.Course,
                EnrollmentYear = s.EnrollmentYear,
                Status = s.Status,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto
        {
            TotalStudents = totalStudents,
            ActiveStudents = activeCount,
            InactiveStudents = inactiveCount,
            GraduatedStudents = graduatedCount,
            CurrentYear = currentYear,
            EnrolledThisYear = enrolledThisYear,
            EnrolledLastYear = enrolledLastYear,
            CourseDistribution = courseDistribution,
            MonthlyEnrollments = monthlyEnrollments,
            RecentStudents = recentStudents
        };
    }
}

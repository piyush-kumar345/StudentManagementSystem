using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.API.Controllers;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.Models;
using StudentManagementSystem.API.Services;

namespace StudentManagementSystem.Tests.Services;

public class DashboardServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Student CreateStudent(
        string enrollmentNumber,
        string firstName,
        string lastName,
        string course,
        int enrollmentYear,
        StudentStatus status,
        DateTime createdAtUtc) => new()
    {
        EnrollmentNumber = enrollmentNumber,
        FirstName = firstName,
        LastName = lastName,
        Email = $"{enrollmentNumber.ToLowerInvariant()}@example.com",
        DateOfBirth = new DateOnly(2004, 1, 15),
        Gender = "Female",
        Course = course,
        EnrollmentYear = enrollmentYear,
        Status = status,
        CreatedAtUtc = createdAtUtc
    };

    [Fact]
    public async Task GetStatsAsync_EmptyDatabase_ReturnsZeroCountsAndEmptyCollections()
    {
        await using var db = CreateInMemoryContext();
        var fixedClock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));
        var service = new DashboardService(db, fixedClock);

        var stats = await service.GetStatsAsync();

        Assert.Equal(0, stats.TotalStudents);
        Assert.Equal(0, stats.ActiveStudents);
        Assert.Equal(0, stats.InactiveStudents);
        Assert.Equal(0, stats.GraduatedStudents);
        Assert.Equal(2026, stats.CurrentYear);
        Assert.Equal(0, stats.EnrolledThisYear);
        Assert.Equal(0, stats.EnrolledLastYear);
        Assert.Empty(stats.CourseDistribution);
        Assert.Empty(stats.RecentStudents);
        Assert.Equal(12, stats.MonthlyEnrollments.Count);
        Assert.All(stats.MonthlyEnrollments, m => Assert.Equal(0, m.Count));
    }

    [Fact]
    public async Task GetStatsAsync_PopulatedDatabase_ComputesStatusAndYearCountsAccurately()
    {
        await using var db = CreateInMemoryContext();
        var fixedClock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));

        db.Students.AddRange(
            CreateStudent("SMS-1", "A", "One", "B.Tech CS", 2026, StudentStatus.Active, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-2", "B", "Two", "B.Tech CS", 2026, StudentStatus.Active, new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-3", "C", "Three", "MBA", 2025, StudentStatus.Inactive, new DateTime(2025, 9, 5, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-4", "D", "Four", "BCA", 2025, StudentStatus.Graduated, new DateTime(2025, 8, 20, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-5", "E", "Five", "BCA", 2024, StudentStatus.Graduated, new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db, fixedClock);
        var stats = await service.GetStatsAsync();

        Assert.Equal(5, stats.TotalStudents);
        Assert.Equal(2, stats.ActiveStudents);
        Assert.Equal(1, stats.InactiveStudents);
        Assert.Equal(2, stats.GraduatedStudents);
        Assert.Equal(2026, stats.CurrentYear);
        Assert.Equal(2, stats.EnrolledThisYear);
        Assert.Equal(2, stats.EnrolledLastYear);
    }

    [Fact]
    public async Task GetStatsAsync_CourseDistribution_GroupsAndOrdersByCountDescending()
    {
        await using var db = CreateInMemoryContext();
        var fixedClock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));

        db.Students.AddRange(
            CreateStudent("SMS-1", "A", "One", "B.Tech CS", 2026, StudentStatus.Active, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-2", "B", "Two", "B.Tech CS", 2026, StudentStatus.Active, new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-3", "C", "Three", "B.Tech CS", 2026, StudentStatus.Active, new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-4", "D", "Four", "MBA", 2026, StudentStatus.Active, new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-5", "E", "Five", "BCA", 2026, StudentStatus.Active, new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db, fixedClock);
        var stats = await service.GetStatsAsync();

        Assert.Equal(3, stats.CourseDistribution.Count);
        Assert.Equal("B.Tech CS", stats.CourseDistribution[0].Course);
        Assert.Equal(3, stats.CourseDistribution[0].StudentCount);
        // Ties ordered alphabetically: BCA then MBA
        Assert.Equal("BCA", stats.CourseDistribution[1].Course);
        Assert.Equal(1, stats.CourseDistribution[1].StudentCount);
        Assert.Equal("MBA", stats.CourseDistribution[2].Course);
        Assert.Equal(1, stats.CourseDistribution[2].StudentCount);
    }

    [Fact]
    public async Task GetStatsAsync_MonthlyEnrollments_IncludesOnlyCurrentCalendarYear()
    {
        await using var db = CreateInMemoryContext();
        var fixedClock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));

        db.Students.AddRange(
            CreateStudent("SMS-1", "Jan", "One", "BCA", 2026, StudentStatus.Active, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-2", "Jan", "Two", "BCA", 2026, StudentStatus.Active, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-3", "Nov", "Three", "BCA", 2026, StudentStatus.Active, new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateStudent("SMS-4", "Prev", "Year", "BCA", 2025, StudentStatus.Active, new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var service = new DashboardService(db, fixedClock);
        var stats = await service.GetStatsAsync();

        Assert.Equal(12, stats.MonthlyEnrollments.Count);
        Assert.Equal("Jan", stats.MonthlyEnrollments[0].MonthName);
        Assert.Equal(2, stats.MonthlyEnrollments[0].Count);
        Assert.Equal(0, stats.MonthlyEnrollments[1].Count);
        Assert.Equal("Nov", stats.MonthlyEnrollments[10].MonthName);
        Assert.Equal(1, stats.MonthlyEnrollments[10].Count);
    }

    [Fact]
    public async Task GetStatsAsync_RecentStudents_ReturnsLatestFiveOrderedByCreatedAtDesc()
    {
        await using var db = CreateInMemoryContext();
        var fixedClock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));

        for (var i = 1; i <= 7; i++)
        {
            db.Students.Add(CreateStudent(
                $"SMS-00{i}",
                $"First{i}",
                $"Last{i}",
                "B.Tech CS",
                2026,
                StudentStatus.Active,
                new DateTime(2026, 11, i, 10, 0, 0, DateTimeKind.Utc)));
        }

        await db.SaveChangesAsync();

        var service = new DashboardService(db, fixedClock);
        var stats = await service.GetStatsAsync();

        Assert.Equal(5, stats.RecentStudents.Count);
        Assert.Equal("SMS-007", stats.RecentStudents[0].EnrollmentNumber);
        Assert.Equal("First7 Last7", stats.RecentStudents[0].FullName);
        Assert.Equal("SMS-003", stats.RecentStudents[4].EnrollmentNumber);
    }

    [Fact]
    public void DashboardController_RequiresAdminRoleAuthorization()
    {
        var authorizeAttr = typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttr);
        Assert.Equal("Admin", authorizeAttr!.Roles);
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.DTOs.Students;
using StudentManagementSystem.API.Exceptions;
using StudentManagementSystem.API.Models;
using StudentManagementSystem.API.Services;

namespace StudentManagementSystem.Tests.Services;

public class StudentServiceTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static CreateStudentDto BuildCreateDto(
        string enrollmentNumber = "SMS-2026-0001",
        string firstName = "Aarav",
        string lastName = "Sharma",
        string email = "aarav.sharma@example.com",
        string course = "B.Tech CS",
        StudentStatus status = StudentStatus.Active) => new()
    {
        EnrollmentNumber = enrollmentNumber,
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        PhoneNumber = "+91-9876543210",
        DateOfBirth = new DateOnly(2004, 5, 14),
        Gender = "Male",
        Course = course,
        EnrollmentYear = 2026,
        Address = "123 Campus Road",
        Status = status
    };

    private static IList<ValidationResult> ValidateDto(object dto)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public async Task CreateAsync_ValidStudent_CreatesAndReturnsStudent()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        var dto = BuildCreateDto();
        dto.EnrollmentNumber = "  SMS-2026-0001  ";
        dto.Email = "  aarav.sharma@example.com  ";

        var created = await service.CreateAsync(dto);

        Assert.True(created.Id > 0);
        Assert.Equal("SMS-2026-0001", created.EnrollmentNumber);
        Assert.Equal("Aarav Sharma", created.FullName);
        Assert.Equal("aarav.sharma@example.com", created.Email);
        Assert.Equal(StudentStatus.Active, created.Status);
        Assert.Null(created.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEnrollmentNumber_ThrowsConflictException()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await service.CreateAsync(BuildCreateDto("SMS-2026-0001", email: "first@example.com"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(BuildCreateDto("sms-2026-0001", email: "second@example.com")));

        Assert.Contains("enrollment number", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsConflictException()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await service.CreateAsync(BuildCreateDto("SMS-2026-0001", email: "Aarav@Example.com"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(BuildCreateDto("SMS-2026-0002", email: "aarav@example.com")));

        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStudent_ReturnsStudent()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        var created = await service.CreateAsync(BuildCreateDto());
        var fetched = await service.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("SMS-2026-0001", fetched.EnrollmentNumber);
        Assert.Equal("Aarav Sharma", fetched.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_MissingStudent_ThrowsNotFoundException()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task UpdateAsync_ValidChanges_UpdatesStudentAndSetsUpdatedAtUtc()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        var created = await service.CreateAsync(BuildCreateDto());

        var updateDto = new UpdateStudentDto
        {
            EnrollmentNumber = created.EnrollmentNumber, // same enrollment should be allowed
            FirstName = "Aarav",
            LastName = "Verma",
            Email = created.Email,
            PhoneNumber = "+91-9999999999",
            DateOfBirth = new DateOnly(2004, 5, 14),
            Gender = "Male",
            Course = "M.Tech AI",
            EnrollmentYear = 2026,
            Address = "Hostel B",
            Status = StudentStatus.Graduated
        };

        var updated = await service.UpdateAsync(created.Id, updateDto);

        Assert.Equal("Aarav Verma", updated.FullName);
        Assert.Equal("M.Tech AI", updated.Course);
        Assert.Equal(StudentStatus.Graduated, updated.Status);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_CollidingWithAnotherStudentEmail_ThrowsConflictException()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await service.CreateAsync(BuildCreateDto("SMS-2026-0001", email: "one@example.com"));
        var second = await service.CreateAsync(BuildCreateDto("SMS-2026-0002", email: "two@example.com"));

        var updateDto = new UpdateStudentDto
        {
            EnrollmentNumber = second.EnrollmentNumber,
            FirstName = second.FirstName,
            LastName = second.LastName,
            Email = "ONE@example.com",
            DateOfBirth = second.DateOfBirth,
            Gender = second.Gender,
            Course = second.Course,
            EnrollmentYear = second.EnrollmentYear,
            Status = StudentStatus.Active
        };

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(second.Id, updateDto));
    }

    [Fact]
    public async Task UpdateAsync_MissingStudent_ThrowsNotFoundException()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        var updateDto = new UpdateStudentDto
        {
            EnrollmentNumber = "SMS-2026-0099",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(2003, 1, 1),
            Gender = "Other",
            Course = "BCA",
            EnrollmentYear = 2025,
            Status = StudentStatus.Active
        };

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(999, updateDto));
    }

    [Fact]
    public async Task DeleteAsync_ExistingStudent_RemovesRecord()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        var created = await service.CreateAsync(BuildCreateDto());
        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_MissingStudent_ThrowsNotFoundException()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(404));
    }

    [Theory]
    [InlineData("priya", "SMS-2026-0002")]
    [InlineData("patel", "SMS-2026-0002")]
    [InlineData("priya patel", "SMS-2026-0002")]
    [InlineData("sms-2026-0003", "SMS-2026-0003")]
    public async Task GetAllAsync_SearchByFirstLastFullNameOrEnrollment_ReturnsMatchingStudents(
        string searchTerm,
        string expectedEnrollment)
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await service.CreateAsync(BuildCreateDto("SMS-2026-0001", "Aarav", "Sharma", "a@example.com"));
        await service.CreateAsync(BuildCreateDto("SMS-2026-0002", "Priya", "Patel", "p@example.com"));
        await service.CreateAsync(BuildCreateDto("SMS-2026-0003", "Rohan", "Mehta", "r@example.com"));

        var result = await service.GetAllAsync(new StudentQueryDto { Search = searchTerm });

        var match = Assert.Single(result.Items);
        Assert.Equal(expectedEnrollment, match.EnrollmentNumber);
    }

    [Fact]
    public async Task GetAllAsync_FilterByCourseAndStatus_ReturnsMatchingStudents()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        await service.CreateAsync(BuildCreateDto("SMS-1", "A", "One", "a1@example.com", "B.Tech CS", StudentStatus.Active));
        await service.CreateAsync(BuildCreateDto("SMS-2", "B", "Two", "b2@example.com", "B.Tech CS", StudentStatus.Graduated));
        await service.CreateAsync(BuildCreateDto("SMS-3", "C", "Three", "c3@example.com", "MBA", StudentStatus.Active));

        var result = await service.GetAllAsync(new StudentQueryDto
        {
            Course = "b.tech cs",
            Status = StudentStatus.Graduated
        });

        var match = Assert.Single(result.Items);
        Assert.Equal("SMS-2", match.EnrollmentNumber);
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsAccuratePageMetadataAndSlice()
    {
        await using var db = CreateInMemoryContext();
        var service = new StudentService(db, NullLogger<StudentService>.Instance);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(BuildCreateDto(
                $"SMS-2026-{i:D4}",
                $"First{i}",
                $"Last{i}",
                $"student{i}@example.com"));
        }

        var page1 = await service.GetAllAsync(new StudentQueryDto { Page = 1, PageSize = 2 });
        var page2 = await service.GetAllAsync(new StudentQueryDto { Page = 2, PageSize = 2 });
        var page3 = await service.GetAllAsync(new StudentQueryDto { Page = 3, PageSize = 2 });

        Assert.Equal(5, page1.TotalRecords);
        Assert.Equal(3, page1.TotalPages);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);
        Assert.Equal(2, page1.Items.Count);

        Assert.Equal(2, page2.PageNumber);
        Assert.True(page2.HasPreviousPage);
        Assert.True(page2.HasNextPage);
        Assert.Equal(2, page2.Items.Count);

        Assert.Single(page3.Items);
        Assert.True(page3.HasPreviousPage);
        Assert.False(page3.HasNextPage);
    }

    [Fact]
    public void CreateStudentDto_AttributeValidation_RejectsMissingAndInvalidFormatFields()
    {
        var invalidDto = new CreateStudentDto
        {
            EnrollmentNumber = "",
            FirstName = new string('A', 61),
            LastName = "",
            Email = "not-an-email",
            DateOfBirth = new DateOnly(2004, 5, 14),
            Gender = "Male",
            Course = "B.Tech CS",
            EnrollmentYear = 2026
        };

        var errors = ValidateDto(invalidDto);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.EnrollmentNumber)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.FirstName)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.LastName)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.Email)));
    }

    [Fact]
    public void CreateStudentDto_DomainValidation_RejectsFutureDateOfBirthAndOutOfRangeEnrollmentYear()
    {
        var dto = BuildCreateDto();
        dto.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        dto.EnrollmentYear = 1900;

        var errors = ValidateDto(dto);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.DateOfBirth)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.EnrollmentYear)));
    }

    [Fact]
    public void CreateStudentDto_Validation_RejectsEnrollmentYearBeforeBirthYear()
    {
        var dto = BuildCreateDto();
        dto.DateOfBirth = new DateOnly(2005, 6, 1);
        dto.EnrollmentYear = 2000;

        var errors = ValidateDto(dto);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStudentDto.EnrollmentYear)));
    }
}

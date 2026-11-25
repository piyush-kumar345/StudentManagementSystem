using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.DTOs.Common;
using StudentManagementSystem.API.DTOs.Students;
using StudentManagementSystem.API.Exceptions;
using StudentManagementSystem.API.Interfaces;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Services;

/// <summary>
/// Implements student CRUD, database-level search, filtering, pagination, and duplicate checks.
/// </summary>
public class StudentService : IStudentService
{
    private static readonly Expression<Func<Student, StudentResponseDto>> ToResponseExpression =
        s => new StudentResponseDto
        {
            Id = s.Id,
            EnrollmentNumber = s.EnrollmentNumber,
            FirstName = s.FirstName,
            LastName = s.LastName,
            FullName = s.FirstName + " " + s.LastName,
            Email = s.Email,
            PhoneNumber = s.PhoneNumber,
            DateOfBirth = s.DateOfBirth,
            Gender = s.Gender,
            Course = s.Course,
            EnrollmentYear = s.EnrollmentYear,
            Address = s.Address,
            Status = s.Status,
            CreatedAtUtc = s.CreatedAtUtc,
            UpdatedAtUtc = s.UpdatedAtUtc
        };

    private readonly AppDbContext _dbContext;
    private readonly ILogger<StudentService> _logger;

    public StudentService(AppDbContext dbContext, ILogger<StudentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResultDto<StudentResponseDto>> GetAllAsync(
        StudentQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<Student> studentsQuery = _dbContext.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            studentsQuery = studentsQuery.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                (s.FirstName + " " + s.LastName).ToLower().Contains(term) ||
                s.EnrollmentNumber.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Course))
        {
            var course = query.Course.Trim().ToLower();
            studentsQuery = studentsQuery.Where(s => s.Course.ToLower() == course);
        }

        if (query.Status.HasValue)
        {
            studentsQuery = studentsQuery.Where(s => s.Status == query.Status.Value);
        }

        var totalRecords = await studentsQuery.CountAsync(cancellationToken);

        var items = await studentsQuery
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponseExpression)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<StudentResponseDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<StudentResponseDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            throw new NotFoundException($"Student with ID {id} was not found.");
        }

        return student;
    }

    public async Task<StudentResponseDto> CreateAsync(
        CreateStudentDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var enrollmentNumber = request.EnrollmentNumber.Trim();
        var email = request.Email.Trim();

        await EnsureUniqueStudentIdentifiersAsync(enrollmentNumber, email, excludeStudentId: null, cancellationToken);

        var student = new Student
        {
            Status = request.Status,
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyInputToEntity(student, request, enrollmentNumber, email);

        _dbContext.Students.Add(student);
        await SaveWithConflictHandlingAsync(enrollmentNumber, email, cancellationToken);

        _logger.LogInformation("Created student ID {StudentId} with enrollment number '{EnrollmentNumber}'.",
            student.Id, student.EnrollmentNumber);

        return MapToResponse(student);
    }

    public async Task<StudentResponseDto> UpdateAsync(
        int id,
        UpdateStudentDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var student = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
        {
            throw new NotFoundException($"Student with ID {id} was not found.");
        }

        var enrollmentNumber = request.EnrollmentNumber.Trim();
        var email = request.Email.Trim();

        await EnsureUniqueStudentIdentifiersAsync(enrollmentNumber, email, excludeStudentId: id, cancellationToken);

        ApplyInputToEntity(student, request, enrollmentNumber, email);
        student.Status = request.Status ?? student.Status;
        student.UpdatedAtUtc = DateTime.UtcNow;

        await SaveWithConflictHandlingAsync(enrollmentNumber, email, cancellationToken);

        _logger.LogInformation("Updated student ID {StudentId}.", student.Id);

        return MapToResponse(student);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var student = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
        {
            throw new NotFoundException($"Student with ID {id} was not found.");
        }

        _dbContext.Students.Remove(student);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted student ID {StudentId}.", id);
    }

    private async Task EnsureUniqueStudentIdentifiersAsync(
        string enrollmentNumber,
        string email,
        int? excludeStudentId,
        CancellationToken cancellationToken)
    {
        var normalizedEnrollment = enrollmentNumber.ToLower();
        var normalizedEmail = email.ToLower();

        var existingConflict = await _dbContext.Students
            .AsNoTracking()
            .Where(s =>
                (!excludeStudentId.HasValue || s.Id != excludeStudentId.Value) &&
                (s.EnrollmentNumber.ToLower() == normalizedEnrollment || s.Email.ToLower() == normalizedEmail))
            .Select(s => new { s.EnrollmentNumber, s.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingConflict is null)
        {
            return;
        }

        if (string.Equals(existingConflict.EnrollmentNumber, enrollmentNumber, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException($"A student with enrollment number '{enrollmentNumber}' already exists.");
        }

        throw new ConflictException($"A student with email '{email}' already exists.");
    }

    private async Task SaveWithConflictHandlingAsync(
        string enrollmentNumber,
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, out var sqlException))
        {
            _logger.LogWarning(ex, "Unique constraint violation while saving student record.");

            var errorText = sqlException?.Message ?? ex.InnerException?.Message ?? string.Empty;
            if (errorText.Contains("IX_Students_EnrollmentNumber", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException($"A student with enrollment number '{enrollmentNumber}' already exists.");
            }

            if (errorText.Contains("IX_Students_Email", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException($"A student with email '{email}' already exists.");
            }

            throw new ConflictException("A student with the same unique identifier already exists.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception, out SqlException? sqlException)
    {
        sqlException = exception.InnerException as SqlException;
        if (sqlException is not null)
        {
            return sqlException.Number is 2601 or 2627;
        }

        return false;
    }

    private static void ApplyInputToEntity(
        Student student,
        StudentWriteDto input,
        string trimmedEnrollmentNumber,
        string trimmedEmail)
    {
        student.EnrollmentNumber = trimmedEnrollmentNumber;
        student.FirstName = input.FirstName.Trim();
        student.LastName = input.LastName.Trim();
        student.Email = trimmedEmail;
        student.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
        student.DateOfBirth = input.DateOfBirth!.Value;
        student.Gender = input.Gender.Trim();
        student.Course = input.Course.Trim();
        student.EnrollmentYear = input.EnrollmentYear!.Value;
        student.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
    }

    private static StudentResponseDto MapToResponse(Student student) => new()
    {
        Id = student.Id,
        EnrollmentNumber = student.EnrollmentNumber,
        FirstName = student.FirstName,
        LastName = student.LastName,
        FullName = $"{student.FirstName} {student.LastName}",
        Email = student.Email,
        PhoneNumber = student.PhoneNumber,
        DateOfBirth = student.DateOfBirth,
        Gender = student.Gender,
        Course = student.Course,
        EnrollmentYear = student.EnrollmentYear,
        Address = student.Address,
        Status = student.Status,
        CreatedAtUtc = student.CreatedAtUtc,
        UpdatedAtUtc = student.UpdatedAtUtc
    };
}

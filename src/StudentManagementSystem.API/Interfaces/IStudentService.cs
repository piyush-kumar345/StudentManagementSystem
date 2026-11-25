using StudentManagementSystem.API.DTOs.Common;
using StudentManagementSystem.API.DTOs.Students;

namespace StudentManagementSystem.API.Interfaces;

/// <summary>
/// Defines student management operations including CRUD, search, filtering, and pagination.
/// </summary>
public interface IStudentService
{
    Task<PagedResultDto<StudentResponseDto>> GetAllAsync(
        StudentQueryDto query,
        CancellationToken cancellationToken = default);

    Task<StudentResponseDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<StudentResponseDto> CreateAsync(
        CreateStudentDto request,
        CancellationToken cancellationToken = default);

    Task<StudentResponseDto> UpdateAsync(
        int id,
        UpdateStudentDto request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}

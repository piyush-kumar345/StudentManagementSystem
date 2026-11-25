using System.ComponentModel.DataAnnotations;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.DTOs.Students;

/// <summary>
/// Query parameters for searching, filtering, and paginating student records.
/// </summary>
public class StudentQueryDto
{
    [StringLength(100, ErrorMessage = "Search query cannot exceed 100 characters.")]
    public string? Search { get; set; }

    [StringLength(100, ErrorMessage = "Course filter cannot exceed 100 characters.")]
    public string? Course { get; set; }

    [EnumDataType(typeof(StudentStatus), ErrorMessage = "Invalid student status filter.")]
    public StudentStatus? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;
}

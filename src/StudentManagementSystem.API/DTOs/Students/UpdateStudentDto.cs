using System.ComponentModel.DataAnnotations;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.DTOs.Students;

/// <summary>
/// Request payload for updating an existing student record.
/// </summary>
public class UpdateStudentDto : StudentWriteDto
{
    [Required(ErrorMessage = "Student status is required.")]
    [EnumDataType(typeof(StudentStatus), ErrorMessage = "Invalid student status.")]
    public StudentStatus? Status { get; set; }
}

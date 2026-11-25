using System.ComponentModel.DataAnnotations;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.DTOs.Students;

/// <summary>
/// Request payload for registering a new student.
/// </summary>
public class CreateStudentDto : StudentWriteDto
{
    [EnumDataType(typeof(StudentStatus), ErrorMessage = "Invalid student status.")]
    public StudentStatus Status { get; set; } = StudentStatus.Active;
}

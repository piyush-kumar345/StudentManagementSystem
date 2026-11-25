using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.API.DTOs.Students;

/// <summary>
/// Shared validated input fields for creating and updating a student record.
/// </summary>
public abstract class StudentWriteDto : IValidatableObject
{
    public const int MinimumEnrollmentYear = 1950;
    private static readonly DateOnly MinimumBirthDate = new(1900, 1, 1);

    [Required(ErrorMessage = "Enrollment number is required.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Enrollment number must be between 2 and 30 characters.")]
    public string EnrollmentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(60, ErrorMessage = "First name cannot exceed 60 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(60, ErrorMessage = "Last name cannot exceed 60 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone number format is invalid.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course is required.")]
    [StringLength(100, ErrorMessage = "Course cannot exceed 100 characters.")]
    public string Course { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enrollment year is required.")]
    public int? EnrollmentYear { get; set; }

    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string? Address { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxEnrollmentYear = DateTime.UtcNow.Year + 1;

        if (DateOfBirth.HasValue)
        {
            if (DateOfBirth.Value > today)
            {
                yield return new ValidationResult(
                    "Date of birth cannot be in the future.",
                    [nameof(DateOfBirth)]);
            }
            else if (DateOfBirth.Value < MinimumBirthDate)
            {
                yield return new ValidationResult(
                    $"Date of birth cannot be earlier than {MinimumBirthDate:yyyy-MM-dd}.",
                    [nameof(DateOfBirth)]);
            }
        }

        if (EnrollmentYear.HasValue)
        {
            if (EnrollmentYear.Value < MinimumEnrollmentYear || EnrollmentYear.Value > maxEnrollmentYear)
            {
                yield return new ValidationResult(
                    $"Enrollment year must be between {MinimumEnrollmentYear} and {maxEnrollmentYear}.",
                    [nameof(EnrollmentYear)]);
            }

            if (DateOfBirth.HasValue && EnrollmentYear.Value < DateOfBirth.Value.Year)
            {
                yield return new ValidationResult(
                    "Enrollment year cannot be earlier than the student's birth year.",
                    [nameof(EnrollmentYear)]);
            }
        }
    }
}

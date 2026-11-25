using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.API.DTOs.Auth;

/// <summary>
/// Request payload for administrator login using username or email and password.
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "Username or email is required.")]
    [StringLength(150, ErrorMessage = "Username or email cannot exceed 150 characters.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Password cannot exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;
}

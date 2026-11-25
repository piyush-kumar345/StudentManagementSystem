namespace StudentManagementSystem.API.DTOs.Auth;

/// <summary>
/// Safe public profile of an authenticated administrator (excludes sensitive fields such as PasswordHash).
/// </summary>
public class AdminProfileDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

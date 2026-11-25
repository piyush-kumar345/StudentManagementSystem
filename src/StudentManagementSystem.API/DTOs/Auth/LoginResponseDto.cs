namespace StudentManagementSystem.API.DTOs.Auth;

/// <summary>
/// Response returned upon successful administrator authentication.
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAtUtc { get; set; }

    public AdminProfileDto Admin { get; set; } = new();
}

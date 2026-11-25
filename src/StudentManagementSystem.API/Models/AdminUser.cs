namespace StudentManagementSystem.API.Models;

/// <summary>
/// Represents an administrator account used to manage the system.
/// Passwords are stored exclusively as cryptographic hashes via PasswordHash.
/// </summary>
public class AdminUser
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Admin";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

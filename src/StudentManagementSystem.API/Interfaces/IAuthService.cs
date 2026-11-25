using StudentManagementSystem.API.DTOs.Auth;

namespace StudentManagementSystem.API.Interfaces;

/// <summary>
/// Defines authentication operations for system administrators.
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<AdminProfileDto> GetCurrentAdminAsync(int adminId, CancellationToken cancellationToken = default);
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentManagementSystem.API.Configuration;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.DTOs.Auth;
using StudentManagementSystem.API.Exceptions;
using StudentManagementSystem.API.Interfaces;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Services;

/// <summary>
/// Handles administrator credential verification using ASP.NET Core PasswordHasher
/// and issues signed JWT Bearer access tokens.
/// </summary>
public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username/email or password.";

    // Dummy PBKDF2 hash used when an account is not found so password verification timing stays consistent.
    private static readonly string DummyPasswordHash =
        new PasswordHasher<AdminUser>().HashPassword(new AdminUser(), "DummyTimingProtection!2026");

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AdminUser> _passwordHasher;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher<AdminUser> passwordHasher,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedIdentifier = request.UsernameOrEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedIdentifier) || string.IsNullOrEmpty(request.Password))
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var admin = await _dbContext.Admins
            .FirstOrDefaultAsync(
                a => a.Username.ToLower() == normalizedIdentifier || a.Email.ToLower() == normalizedIdentifier,
                cancellationToken);

        if (admin is null)
        {
            _passwordHasher.VerifyHashedPassword(new AdminUser(), DummyPasswordHash, request.Password);
            _logger.LogWarning("Failed admin login attempt.");
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed password verification for admin ID {AdminId}.", admin.Id);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.PasswordHash = _passwordHasher.HashPassword(admin, request.Password);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
        var accessToken = GenerateJwtToken(admin, expiresAtUtc);

        _logger.LogInformation("Admin ID {AdminId} authenticated successfully.", admin.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAtUtc,
            Admin = MapToProfile(admin)
        };
    }

    public async Task<AdminProfileDto> GetCurrentAdminAsync(
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await _dbContext.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);

        if (admin is null)
        {
            throw new UnauthorizedException("Authenticated admin account no longer exists.");
        }

        return MapToProfile(admin);
    }

    private string GenerateJwtToken(AdminUser admin, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, admin.Username),
            new(ClaimTypes.Name, admin.Username),
            new(JwtRegisteredClaimNames.Email, admin.Email),
            new(ClaimTypes.Role, admin.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AdminProfileDto MapToProfile(AdminUser admin) => new()
    {
        Id = admin.Id,
        Username = admin.Username,
        Email = admin.Email,
        FullName = admin.FullName,
        Role = admin.Role
    };
}

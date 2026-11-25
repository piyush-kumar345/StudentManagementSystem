using System.Text;

namespace StudentManagementSystem.API.Configuration;

/// <summary>
/// Strongly-typed configuration settings for JWT Bearer token generation and validation.
/// The signing Key must be supplied via .NET User Secrets or environment variables.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Validates required JWT configuration and throws a descriptive exception if invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException(
                "JWT signing key 'Jwt:Key' is not configured. " +
                "For local development, run: dotnet user-secrets set \"Jwt:Key\" \"<random-secret-at-least-32-chars>\". " +
                "For deployment, set the 'Jwt__Key' environment variable.");
        }

        if (Encoding.UTF8.GetByteCount(Key) < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"JWT signing key 'Jwt:Key' must be at least {MinimumKeyBytes} bytes (256 bits) for HMAC-SHA256.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("JWT setting 'Jwt:Issuer' is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JWT setting 'Jwt:Audience' is required.");
        }

        if (ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("JWT setting 'Jwt:ExpiryMinutes' must be greater than zero.");
        }
    }
}

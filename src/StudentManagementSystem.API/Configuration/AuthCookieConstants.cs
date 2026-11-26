namespace StudentManagementSystem.API.Configuration;

/// <summary>
/// Centralizes settings for the HttpOnly JWT authentication cookie used by the browser frontend.
/// </summary>
public static class AuthCookieConstants
{
    public const string CookieName = "sms_auth_token";

    public static CookieOptions CreateCookieOptions(
        HttpContext httpContext,
        IHostEnvironment environment,
        DateTimeOffset? expiresAtUtc = null)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = httpContext.Request.IsHttps || !environment.IsDevelopment(),
            IsEssential = true,
            Path = "/",
            Expires = expiresAtUtc
        };
    }
}

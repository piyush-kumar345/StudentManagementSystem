using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.API.Configuration;
using StudentManagementSystem.API.DTOs.Auth;
using StudentManagementSystem.API.Exceptions;
using StudentManagementSystem.API.Interfaces;

namespace StudentManagementSystem.API.Controllers;

/// <summary>
/// Exposes administrator authentication endpoints supporting both JWT Bearer headers and HttpOnly cookies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _environment;

    public AuthController(IAuthService authService, IHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    /// <summary>
    /// Authenticates an administrator using username or email and password,
    /// returning a JWT Bearer token and setting a secure HttpOnly authentication cookie.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        Response.Cookies.Append(
            AuthCookieConstants.CookieName,
            result.AccessToken,
            AuthCookieConstants.CreateCookieOptions(HttpContext, _environment, result.ExpiresAtUtc));

        return Ok(result);
    }

    /// <summary>
    /// Clears the HttpOnly authentication cookie for the current browser session.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            AuthCookieConstants.CookieName,
            AuthCookieConstants.CreateCookieOptions(HttpContext, _environment));

        return NoContent();
    }

    /// <summary>
    /// Returns the profile of the currently authenticated administrator.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AdminProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminProfileDto>> GetCurrentAdmin(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var adminId))
        {
            throw new UnauthorizedException("Invalid authentication token.");
        }

        var profile = await _authService.GetCurrentAdminAsync(adminId, cancellationToken);
        return Ok(profile);
    }
}

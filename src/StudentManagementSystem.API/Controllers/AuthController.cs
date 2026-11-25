using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.API.DTOs.Auth;
using StudentManagementSystem.API.Exceptions;
using StudentManagementSystem.API.Interfaces;

namespace StudentManagementSystem.API.Controllers;

/// <summary>
/// Exposes administrator authentication endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates an administrator using username or email and password, returning a JWT Bearer token.
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
        return Ok(result);
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

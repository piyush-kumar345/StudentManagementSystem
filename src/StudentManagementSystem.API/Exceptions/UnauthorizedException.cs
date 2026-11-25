namespace StudentManagementSystem.API.Exceptions;

/// <summary>
/// Thrown when authentication fails or credentials are invalid. Maps to HTTP 401.
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(message, StatusCodes.Status401Unauthorized, "Unauthorized")
    {
    }
}

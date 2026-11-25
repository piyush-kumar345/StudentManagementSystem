namespace StudentManagementSystem.API.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with existing data (e.g., duplicate enrollment number). Maps to HTTP 409.
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, StatusCodes.Status409Conflict, "Resource Conflict")
    {
    }
}

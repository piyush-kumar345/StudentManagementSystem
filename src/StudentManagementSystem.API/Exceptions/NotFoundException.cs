namespace StudentManagementSystem.API.Exceptions;

/// <summary>
/// Thrown when a requested entity or resource does not exist. Maps to HTTP 404.
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, StatusCodes.Status404NotFound, "Resource Not Found")
    {
    }
}

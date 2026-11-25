namespace StudentManagementSystem.API.Exceptions;

/// <summary>
/// Base class for domain exceptions that map cleanly to HTTP status codes.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode, string title)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }

    public string Title { get; }
}

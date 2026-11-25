using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.API.Exceptions;

namespace StudentManagementSystem.API.Middleware;

/// <summary>
/// Catches unhandled exceptions across the HTTP request pipeline and returns
/// standardized RFC 7807 ProblemDetails JSON responses without leaking sensitive details.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Title, appEx.Message),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, "Invalid Request", argEx.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            )
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled application exception ({StatusCode}) for {Method} {Path}: {Message}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (_environment.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}

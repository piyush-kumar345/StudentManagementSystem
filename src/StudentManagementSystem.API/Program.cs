using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Controllers and serialize enums as readable strings (e.g., "Active").
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Register RFC 7807 ProblemDetails support for standardized API error responses.
builder.Services.AddProblemDetails();

// Configure Entity Framework Core with SQL Server.
// Connection string must be supplied via .NET User Secrets or environment variables.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'ConnectionStrings:DefaultConnection' is not configured. " +
        "For local development, run: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<your-connection-string>\"");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Swagger / OpenAPI documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Management System API",
        Version = "v1",
        Description = "ASP.NET Core Web API (.NET 10) for managing student records and dashboard analytics."
    });
});

var app = builder.Build();

// Global exception handling middleware should run first in the pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Management System API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Enable default files (index.html) and static file hosting from wwwroot/.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

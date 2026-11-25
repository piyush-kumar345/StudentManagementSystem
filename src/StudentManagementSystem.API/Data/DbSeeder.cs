using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Data;

/// <summary>
/// Safely provisions an initial administrator account using configuration values supplied via
/// .NET User Secrets (development) or environment variables (deployment).
/// Never falls back to hardcoded default credentials.
/// </summary>
public static class DbSeeder
{
    public const int MinimumSeedPasswordLength = 8;

    public static async Task SeedInitialAdminAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DbSeeder");

        if (await dbContext.Admins.AnyAsync(cancellationToken))
        {
            return;
        }

        var username = configuration["AdminSeed:Username"]?.Trim();
        var email = configuration["AdminSeed:Email"]?.Trim();
        var fullName = configuration["AdminSeed:FullName"]?.Trim();
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No admin accounts exist and 'AdminSeed' configuration is incomplete. " +
                "Provide AdminSeed:Username, AdminSeed:Email, and AdminSeed:Password via User Secrets or environment variables to provision an initial admin.");
            return;
        }

        if (password.Length < MinimumSeedPasswordLength)
        {
            throw new InvalidOperationException(
                $"Configured 'AdminSeed:Password' must be at least {MinimumSeedPasswordLength} characters long.");
        }

        var admin = new AdminUser
        {
            Username = username,
            Email = email,
            FullName = string.IsNullOrWhiteSpace(fullName) ? "System Administrator" : fullName,
            Role = "Admin",
            CreatedAtUtc = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        dbContext.Admins.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Initial admin account provisioned for username '{Username}'.", admin.Username);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentManagementSystem.API.Configuration;
using StudentManagementSystem.API.Data;
using StudentManagementSystem.API.DTOs.Auth;
using StudentManagementSystem.API.Exceptions;
using StudentManagementSystem.API.Models;
using StudentManagementSystem.API.Services;

namespace StudentManagementSystem.Tests.Services;

public class AuthServiceTests
{
    private static JwtOptions CreateValidJwtOptions() => new()
    {
        Key = "TestSigningKey_AtLeast_32_Characters_Long_For_HMACSHA256!",
        Issuer = "StudentManagementSystem.API",
        Audience = "StudentManagementSystem.Client",
        ExpiryMinutes = 60
    };

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AdminUser SeedAdmin(
        AppDbContext context,
        IPasswordHasher<AdminUser> hasher,
        string plainPassword = "StrongTestPassword!2026")
    {
        var admin = new AdminUser
        {
            Username = "sysadmin",
            Email = "admin@sms.local",
            FullName = "System Administrator",
            Role = "Admin",
            CreatedAtUtc = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, plainPassword);

        context.Admins.Add(admin);
        context.SaveChanges();
        return admin;
    }

    [Fact]
    public async Task LoginAsync_WithValidUsernameAndPassword_ReturnsValidJwtTokenAndProfile()
    {
        await using var dbContext = CreateInMemoryContext();
        var hasher = new PasswordHasher<AdminUser>();
        var admin = SeedAdmin(dbContext, hasher, "CorrectPassword#2026");
        var jwtOptions = CreateValidJwtOptions();

        var service = new AuthService(
            dbContext,
            hasher,
            Options.Create(jwtOptions),
            NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            UsernameOrEmail = "sysadmin",
            Password = "CorrectPassword#2026"
        });

        Assert.Equal("Bearer", result.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(admin.Id, result.Admin.Id);
        Assert.Equal("sysadmin", result.Admin.Username);
        Assert.Equal("admin@sms.local", result.Admin.Email);
        Assert.Equal("Admin", result.Admin.Role);

        // Validate the generated JWT using the same TokenValidationParameters rules as the API.
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(
            result.AccessToken,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
            },
            out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(admin.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("sysadmin", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("Admin", principal.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public async Task LoginAsync_WithValidEmailCaseInsensitive_ReturnsToken()
    {
        await using var dbContext = CreateInMemoryContext();
        var hasher = new PasswordHasher<AdminUser>();
        SeedAdmin(dbContext, hasher, "CorrectPassword#2026");

        var service = new AuthService(
            dbContext,
            hasher,
            Options.Create(CreateValidJwtOptions()),
            NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            UsernameOrEmail = "  ADMIN@SMS.LOCAL  ",
            Password = "CorrectPassword#2026"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("sysadmin", result.Admin.Username);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsGenericUnauthorizedException()
    {
        await using var dbContext = CreateInMemoryContext();
        var hasher = new PasswordHasher<AdminUser>();
        SeedAdmin(dbContext, hasher, "CorrectPassword#2026");

        var service = new AuthService(
            dbContext,
            hasher,
            Options.Create(CreateValidJwtOptions()),
            NullLogger<AuthService>.Instance);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequestDto
            {
                UsernameOrEmail = "sysadmin",
                Password = "WrongPassword!"
            }));

        Assert.Equal("Invalid username/email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ThrowsSameGenericUnauthorizedException()
    {
        await using var dbContext = CreateInMemoryContext();
        var hasher = new PasswordHasher<AdminUser>();
        SeedAdmin(dbContext, hasher, "CorrectPassword#2026");

        var service = new AuthService(
            dbContext,
            hasher,
            Options.Create(CreateValidJwtOptions()),
            NullLogger<AuthService>.Instance);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequestDto
            {
                UsernameOrEmail = "nonexistent",
                Password = "AnyPassword!"
            }));

        Assert.Equal("Invalid username/email or password.", ex.Message);
    }

    [Fact]
    public void ValidateToken_WithTamperedSigningKey_ThrowsSecurityTokenException()
    {
        var jwtOptions = CreateValidJwtOptions();
        var handler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, "Admin")],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                SecurityAlgorithms.HmacSha256));

        var token = handler.WriteToken(tokenDescriptor);

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("DifferentKey_AtLeast_32_Characters_Long_12345!"))
                },
                out _));
    }

    [Fact]
    public async Task GetCurrentAdminAsync_WithExistingAdmin_ReturnsProfileWithoutPasswordHash()
    {
        await using var dbContext = CreateInMemoryContext();
        var hasher = new PasswordHasher<AdminUser>();
        var admin = SeedAdmin(dbContext, hasher);

        var service = new AuthService(
            dbContext,
            hasher,
            Options.Create(CreateValidJwtOptions()),
            NullLogger<AuthService>.Instance);

        var profile = await service.GetCurrentAdminAsync(admin.Id);

        Assert.Equal(admin.Id, profile.Id);
        Assert.Equal(admin.Username, profile.Username);
        Assert.Equal(admin.Email, profile.Email);
        Assert.Equal(admin.FullName, profile.FullName);
        Assert.Equal("Admin", profile.Role);
        Assert.Null(typeof(AdminProfileDto).GetProperty("PasswordHash"));
    }

    [Fact]
    public void JwtOptions_Validate_ThrowsWhenKeyMissingOrTooShort()
    {
        var missingKeyOptions = CreateValidJwtOptions();
        missingKeyOptions.Key = "";
        Assert.Throws<InvalidOperationException>(() => missingKeyOptions.Validate());

        var shortKeyOptions = CreateValidJwtOptions();
        shortKeyOptions.Key = "too-short";
        Assert.Throws<InvalidOperationException>(() => shortKeyOptions.Validate());
    }

    [Fact]
    public async Task DbSeeder_WhenAdminSeedMissing_DoesNotCreateHardcodedAdmin()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        await DbSeeder.SeedInitialAdminAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.Admins.ToListAsync());
    }

    [Fact]
    public async Task DbSeeder_WhenAdminSeedConfigured_HashesPasswordAndCreatesAdmin()
    {
        var dbName = Guid.NewGuid().ToString();
        var configValues = new Dictionary<string, string?>
        {
            ["AdminSeed:Username"] = "initialadmin",
            ["AdminSeed:Email"] = "initial@sms.local",
            ["AdminSeed:FullName"] = "Initial Admin",
            ["AdminSeed:Password"] = "SeededPass!2026"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(configValues).Build());

        using var provider = services.BuildServiceProvider();
        await DbSeeder.SeedInitialAdminAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
        var admin = await db.Admins.SingleAsync();

        Assert.Equal("initialadmin", admin.Username);
        Assert.NotEqual("SeededPass!2026", admin.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(admin, admin.PasswordHash, "SeededPass!2026"));
    }
}

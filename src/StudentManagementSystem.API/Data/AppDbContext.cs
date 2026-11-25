using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Data;

/// <summary>
/// Entity Framework Core database context for the Student Management System.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<AdminUser> Admins => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discover and apply all IEntityTypeConfiguration<T> classes in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

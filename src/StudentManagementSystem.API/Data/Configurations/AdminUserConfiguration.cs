using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Data.Configurations;

/// <summary>
/// Configures the SQL Server schema, constraints, and indexes for the AdminUser entity.
/// </summary>
public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("Admins");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Role)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("Admin");

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => a.Username)
            .IsUnique()
            .HasDatabaseName("IX_Admins_Username");

        builder.HasIndex(a => a.Email)
            .IsUnique()
            .HasDatabaseName("IX_Admins_Email");
    }
}

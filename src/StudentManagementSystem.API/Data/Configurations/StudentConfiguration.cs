using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentManagementSystem.API.Models;

namespace StudentManagementSystem.API.Data.Configurations;

/// <summary>
/// Configures the SQL Server schema, constraints, and indexes for the Student entity.
/// </summary>
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EnrollmentNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(s => s.DateOfBirth)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(s => s.Gender)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Course)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.EnrollmentYear)
            .IsRequired();

        builder.Property(s => s.Address)
            .HasMaxLength(300);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(StudentStatus.Active)
            .HasSentinel((StudentStatus)0);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired(false);

        builder.HasIndex(s => s.EnrollmentNumber)
            .IsUnique()
            .HasDatabaseName("IX_Students_EnrollmentNumber");

        builder.HasIndex(s => s.Email)
            .IsUnique()
            .HasDatabaseName("IX_Students_Email");

        builder.HasIndex(s => new { s.FirstName, s.LastName })
            .HasDatabaseName("IX_Students_Name");
    }
}

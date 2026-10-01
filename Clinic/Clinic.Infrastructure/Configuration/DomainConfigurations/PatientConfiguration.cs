using Clinic.Domain.Models;
using Clinic.Infrastructure.identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Configuration.DomainConfigurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);

        // DateOnly maps to SQL Server 'date' by default — no time, no time zone.
        builder.Property(p => p.DateOfBirth).IsRequired();

        builder.Property(p => p.Gender).HasMaxLength(20);

        // README 6: phone is required clinic contact data; email and emergency contact
        // are recorded when the patient has them.
        builder.Property(p => p.Phone).IsRequired().HasMaxLength(30);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.Address).HasMaxLength(200);
        builder.Property(p => p.EmergencyContact).HasMaxLength(100);

        builder.Property(p => p.IsActive).IsRequired();

        // UserId is optional. The previous IsRequired() contradicted the entity's
        // `string? UserId` and made it impossible for a receptionist to register a
        // walk-in patient who has no login account.
        builder.Property(p => p.UserId).IsRequired(false);

        builder.HasOne<AppUser>()
               .WithOne()
               .HasForeignKey<Patient>(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // A filtered unique index. Without the filter, SQL Server treats NULLs as equal
        // for uniqueness purposes, so the second account-less patient would violate the
        // index. With it, at most one patient may be linked to any given user, and
        // unlinked patients are unconstrained.
        builder.HasIndex(p => p.UserId)
               .IsUnique()
               .HasFilter("[UserId] IS NOT NULL");

        // Patients are looked up by name at the front desk far more often than by id.
        builder.HasIndex(p => new { p.LastName, p.FirstName });
        builder.HasIndex(p => p.Phone);
    }
}

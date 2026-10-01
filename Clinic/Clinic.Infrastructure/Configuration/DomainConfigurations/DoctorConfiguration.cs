using Clinic.Domain.Models;
using Clinic.Infrastructure.identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Configuration.DomainConfigurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.LastName).IsRequired().HasMaxLength(100);

        // README 5.2 lists Email and Phone on the doctor record.
        builder.Property(d => d.Email).IsRequired().HasMaxLength(256);
        builder.Property(d => d.Phone).IsRequired().HasMaxLength(30);

        builder.Property(d => d.SpecialtyId).IsRequired();
        builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(50);

        // Money must never be a float. HasPrecision(18,2) maps to decimal(18,2) so a fee
        // of 149.99 stores and compares exactly.
        builder.Property(d => d.ConsultationFee).HasPrecision(18, 2).IsRequired();

        builder.Property(d => d.IsActive).IsRequired();

        // A licence number identifies one practitioner nationally, so it must be unique.
        builder.HasIndex(d => d.LicenseNumber).IsUnique();

        // Restrict, not Cascade: deleting a specialty must never silently delete the
        // doctors practising it. The API deactivates specialties instead.
        builder.HasOne(d => d.Specialty)
               .WithMany(s => s.Doctors)
               .HasForeignKey(d => d.SpecialtyId)
               .OnDelete(DeleteBehavior.Restrict);

        // One Identity account maps to at most one doctor. UserId is required here
        // (unlike Patient) because a doctor always signs in to the system.
        builder.HasOne<AppUser>()
               .WithOne()
               .HasForeignKey<Doctor>(d => d.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.UserId).IsUnique();

        // Listing the doctors of a specialty is the most common query in the booking flow.
        builder.HasIndex(d => new { d.SpecialtyId, d.IsActive });
    }
}

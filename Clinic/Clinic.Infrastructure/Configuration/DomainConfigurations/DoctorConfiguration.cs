
using Clinic.Domain.Models;
using Clinic.Infrastructure.identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Configuration.DomainConfigurations
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.UserId).IsRequired();
            builder.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.LastName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.SpecialtyId).IsRequired();
            builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(50);
            builder.Property(d => d.ConsultationFee).IsRequired();
            builder.Property(d => d.IsActive).IsRequired();
            // Indexes for performance optimization
            builder.HasIndex(d => d.LicenseNumber).IsUnique(); // Assuming one-to-one relationship with User

            // Configure the relationship with Specialty
            builder.HasOne(d => d.Specialty)
                   .WithMany(s => s.Doctors)
                   .HasForeignKey(d => d.SpecialtyId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of Specialty if Doctors exist
             builder.HasOne<AppUser>()
                    .WithOne()
                    .HasForeignKey<Doctor>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of User if Doctor exists
        }
        
    }
}
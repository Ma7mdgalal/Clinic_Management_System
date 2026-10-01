using Clinic.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Configuration.DomainConfigurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        // README 5.3: "Specialty names cannot be duplicated." This index is the actual
        // enforcement — the service's NameExistsAsync check is only there to turn the
        // common case into a clean 409 instead of a database error, and it can lose a
        // race between two concurrent creates. The index cannot.
        //
        // The comparison is case-insensitive because SQL Server's default collation
        // (…_CI_AS) is, so 'cardiology' and 'Cardiology' collide as intended and the
        // repository can filter with a plain equality that uses this index.
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

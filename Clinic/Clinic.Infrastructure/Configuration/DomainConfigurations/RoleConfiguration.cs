using Clinic.Infrastructure.identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Configuration.DomainConfigurations;

// Infrastructure/Persistence/Configurations/RoleConfiguration.cs
public class RoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
public void Configure(EntityTypeBuilder<ApplicationRole> builder)
{
    builder.HasData(
        new ApplicationRole
        {
            Id = "1a5e1a1a-0000-0000-0000-000000000001",
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = "1a5e1a1a-0000-0000-0000-000000001001"
        },
        new ApplicationRole
        {
            Id = "1a5e1a1a-0000-0000-0000-000000000002",
            Name = "Doctor",
            NormalizedName = "DOCTOR",
            ConcurrencyStamp = "1a5e1a1a-0000-0000-0000-000000001002"
        },
        new ApplicationRole
        {
            Id = "1a5e1a1a-0000-0000-0000-000000000003",
            Name = "Patient",
            NormalizedName = "PATIENT",
            ConcurrencyStamp = "1a5e1a1a-0000-0000-0000-000000001003"
        },
        new ApplicationRole
        {
            Id = "1a5e1a1a-0000-0000-0000-000000000004",
            Name = "Receptionist",
            NormalizedName = "RECEPTIONIST",
            ConcurrencyStamp = "1a5e1a1a-0000-0000-0000-000000001004"
        }
    );
}
}
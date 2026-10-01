using Clinic.Application.Common;
using Clinic.Application.Interfaces.Repository;
using Clinic.Application.Interfaces.Services;
using Clinic.Infrastructure.identity;
using Clinic.Infrastructure.identity.Services;
using Clinic.Infrastructure.Persistence;
using Clinic.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Infrastructure.Configuration;

public static class Extensions
{
    public static IServiceCollection ConfigureDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Fails loudly at startup rather than on the first request. The connection string
        // lives in user secrets, not appsettings.json, because it carries credentials.
        var connectionString = configuration.GetConnectionString("sqlConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'sqlConnection' was not found. Set it with: " +
                "dotnet user-secrets set \"ConnectionStrings:sqlConnection\" \"<value>\"");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = false;

            // Two accounts must not share an email: it is how a person is identified for
            // password resets, and Identity does not enforce this by default.
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BootstrapAdminSettings>(
            configuration.GetSection(BootstrapAdminSettings.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<JwtService>();
        services.AddScoped<IdentitySeeder>();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        return services;
    }
}

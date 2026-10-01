
using Clinic.Application.Interfaces.Services;
using Clinic.Application.Services;
using Microsoft.Extensions.DependencyInjection;


namespace Clinic.Application.Configuration;

public static class Extensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IPatientService, PatientService>();

    }

}
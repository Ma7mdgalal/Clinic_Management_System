using System.Text;
using Clinic.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Clinic.Api.Extensions;

public static class Configs
{
    public static IServiceCollection ConfigureJwt(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bind + validate + ValidateOnStart. The previous version called
        // Get<JwtSettings>()! and trusted the null-forgiving operator: with the Jwt
        // section missing, that produced a NullReferenceException deep inside DI, and
        // with a short key it produced a cryptography error on the first login.
        // ValidateOnStart turns both into a clear failure before the app accepts traffic.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // This callback runs when JwtBearerOptions is first resolved, which is
                // after the startup validator above has already run — so the values here
                // are guaranteed present and well-formed.
                var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // no grace period on expiry
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireAdmin, p => p.RequireRole(Roles.Admin));
            options.AddPolicy(Policies.RequireDoctor, p => p.RequireRole(Roles.Doctor));
            options.AddPolicy(Policies.RequirePatient, p => p.RequireRole(Roles.Patient));

            // Registered because Receptionist exists in Roles and is seeded, but had no
            // policy — any endpoint written for the front desk would have had to fall
            // back to a magic string.
            options.AddPolicy(Policies.RequireReceptionist, p => p.RequireRole(Roles.Receptionist));

            options.AddPolicy(Policies.RequireClinicStaff,
                p => p.RequireRole(Roles.Admin, Roles.Receptionist));
            options.AddPolicy(Policies.RequireDoctorOrAdmin, p => p.RequireRole(Roles.Admin,Roles.Doctor));
        });

        return services;
    }

    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Clinic Management System API",
                Version = "v1",
                Description = "Clinic operations: authentication, specialties, doctors, patients."
            });

            // Without this, Swagger UI can describe the protected endpoints but cannot
            // call them: there is no way to attach a bearer token, so every admin-only
            // action returns 401 from the UI and appears broken.
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the token from /api/auth/login only — Swagger adds the \"Bearer \" prefix itself."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}


using Clinic.Application.Common;
using Clinic.Application.Interfaces;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.identity;
using Clinic.Infrastructure.identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Infrastructure.Configuration
{
    public static class Extensions
    {
        public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("sqlConnection")
                ?? throw new InvalidOperationException("Connection string 'sqlConnection' was not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            


        }
        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddIdentityCore<AppUser>(options =>
           {
               options.Password.RequireDigit = true;
               options.Password.RequiredLength = 8;
               options.Password.RequireNonAlphanumeric = false;
               options.Password.RequireUppercase = true;
               options.Password.RequireLowercase = false;
           })
           .AddRoles<ApplicationRole>()
           .AddEntityFrameworkStores<AppDbContext>();

        }

        public static void AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<JwtService>();
        }

       


    }
}

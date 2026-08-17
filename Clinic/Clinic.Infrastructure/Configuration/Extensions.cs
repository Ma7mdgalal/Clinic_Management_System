
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.identity;
using Microsoft.AspNetCore.Identity;
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
    }
}

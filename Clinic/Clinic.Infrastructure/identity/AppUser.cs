
using Microsoft.AspNetCore.Identity;

namespace Clinic.Infrastructure.identity
{
    public class AppUser : IdentityUser<string>
    {
        public AppUser()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
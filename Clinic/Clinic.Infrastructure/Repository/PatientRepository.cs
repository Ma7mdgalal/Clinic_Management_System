using Clinic.Application.Interfaces.Repository;
using Clinic.Domain.Models;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repository;

public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) :base(context){}

    public async Task<IReadOnlyList<Patient>> GetAllAsync(bool includeInactive)
    {
         var query = DbContext.Patients.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.ToListAsync();
    }
}

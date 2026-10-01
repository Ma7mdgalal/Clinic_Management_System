using Clinic.Application.Interfaces.Repository;
using Clinic.Domain.Models;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repository;

public class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
{
    public DoctorRepository(AppDbContext context) :base(context){}
    
    public async Task<Doctor?> GetByLicenseNumberAsync(string LicenseNumber) =>
        await DbContext.Doctors
            .AsNoTracking()
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(s => s.LicenseNumber == LicenseNumber);

    public async Task<IReadOnlyList<Doctor>> GetAllAsync(bool includeInactive)
    {
        var query = DbContext.Doctors.AsNoTracking().Include(d => d.Specialty).AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.ToListAsync();
    }
}

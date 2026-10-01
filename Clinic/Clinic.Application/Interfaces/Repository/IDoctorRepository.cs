using Clinic.Domain.Models;

namespace Clinic.Application.Interfaces.Repository;

public interface IDoctorRepository:IGenericRepository<Doctor>
{
    // Task<Specialty?> GetByNameAsync(string name);
    Task<Doctor?> GetByLicenseNumberAsync(string LicenseNumber);
    Task<IReadOnlyList<Doctor>> GetAllAsync(bool includeInactive);

        
} 


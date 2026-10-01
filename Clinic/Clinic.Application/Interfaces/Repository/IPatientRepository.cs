using Clinic.Domain.Models;

namespace Clinic.Application.Interfaces.Repository;

public interface IPatientRepository:IGenericRepository<Patient>
{
    Task<IReadOnlyList<Patient>> GetAllAsync(bool includeInactive);

        
} 


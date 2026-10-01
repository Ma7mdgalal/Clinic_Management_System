using Clinic.Application.Common;
using Clinic.Application.DTOs.Doctors;

namespace Clinic.Application.Interfaces.Services;

public interface IDoctorService
{
    Task<PagedResult<DoctorResponse>> GetAllAsync(bool includeInactive = false, string? search = null, int pageNumber = 1, int pageSize = 10);

    Task<DoctorResponse> GetByIdAsync(Guid id);

    Task<DoctorResponse> GetByLicenseNumberAsync(string name);

    Task<DoctorResponse> CreateAsync(CreateDoctorRequest request);

    Task<DoctorResponse> UpdateAsync(Guid id, UpdateDoctorRequest request);
    
    Task DeactivateAsync(Guid id);
}


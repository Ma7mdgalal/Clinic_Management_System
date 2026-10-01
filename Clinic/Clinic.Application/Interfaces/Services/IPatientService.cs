using Clinic.Application.Common;
using Clinic.Application.DTOs.Patients;

namespace Clinic.Application.Interfaces.Services;

public interface IPatientService
{
    Task<PagedResult<PatientResponse>> GetAllAsync(bool includeInactive = false, string? search = null, int pageNumber = 1, int pageSize = 10);

    Task<PatientResponse> GetByIdAsync(Guid id);

    Task<PatientResponse> CreateAsync(CreatePatientRequest request);

    Task<PatientResponse> UpdateAsync(Guid id, UpdatePatientRequest request);
    
    Task DeactivateAsync(Guid id);

    Task ActivateAsync(Guid id);
}


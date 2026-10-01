using Clinic.Application.Common;
using Clinic.Application.DTOs.Specialties;
using Clinic.Domain.Models;

namespace Clinic.Application.Interfaces.Services;

/// <summary>
/// Specialty management (CMS-010).
/// </summary>
/// <remarks>
/// No method returns null and none returns bool. Missing resources raise
/// NotFoundException and duplicate names raise ConflictException, which the API's
/// exception handler turns into 404 and 409. A bool return cannot distinguish
/// "no such specialty" from "the name is taken", so the controller could only ever
/// guess at a status code.
/// </remarks>
public interface ISpecialtyService
{
    Task<PagedResult<SpecialtyResponse>> GetAllAsync(bool includeInactive = false, string? search = null, int pageNumber = 1, int pageSize = 10);

    /// <exception cref="Common.Exceptions.NotFoundException">No specialty with this id.</exception>
    Task<SpecialtyResponse> GetByIdAsync(Guid id);

    /// <exception cref="Common.Exceptions.NotFoundException">No specialty with this name.</exception>
    Task<SpecialtyResponse> GetByNameAsync(string name);

    /// <exception cref="Common.Exceptions.ConflictException">The name is already in use.</exception>
    Task<SpecialtyResponse> CreateAsync(CreateSpecialtyRequest request);

    /// <exception cref="Common.Exceptions.NotFoundException">No specialty with this id.</exception>
    /// <exception cref="Common.Exceptions.ConflictException">Another specialty owns this name.</exception>
    Task<SpecialtyResponse> UpdateAsync(Guid id, UpdateSpecialtyRequest request);

    /// <summary>
    /// Deactivates the specialty (README 5.3 asks for deactivation, never deletion).
    /// A hard delete would orphan or cascade into every doctor who holds this specialty
    /// and break the history of appointments already booked under it. Idempotent:
    /// deactivating an already-inactive specialty succeeds.
    /// </summary>
    /// <exception cref="Common.Exceptions.NotFoundException">No specialty with this id.</exception>
    Task DeactivateAsync(Guid id);
}

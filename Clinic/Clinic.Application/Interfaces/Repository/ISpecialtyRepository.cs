using Clinic.Domain.Models;

namespace Clinic.Application.Interfaces.Repository;

public interface ISpecialtyRepository : IGenericRepository<Specialty>
{
    Task<Specialty?> GetByNameAsync(string name);

    /// <summary>
    /// True if another specialty already uses this name.
    /// </summary>
    /// <param name="excludeId">
    /// The specialty being updated, if any. Without this, renaming "Cardiology" to
    /// "Cardiology" (or only changing its description) would match its own row and be
    /// rejected as a duplicate.
    /// </param>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null); 

    Task<IReadOnlyList<Specialty>> GetAllAsync(bool includeInactive);
}

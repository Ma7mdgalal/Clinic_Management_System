using Clinic.Application.Interfaces.Repository;
using Clinic.Domain.Models;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repository;

public class SpecialtyRepository : GenericRepository<Specialty>, ISpecialtyRepository
{
    public SpecialtyRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// Looks a specialty up by name.
    /// </summary>
    /// <remarks>
    /// The filter is a plain equality, not <c>s.Name.ToLower() == name.ToLower()</c>.
    /// Wrapping a column in LOWER() makes the predicate non-sargable: SQL Server cannot
    /// use the unique index on Name and falls back to scanning and lowering every row.
    /// It is also redundant here, because the default collation is already
    /// case-insensitive — the LOWER() call bought nothing and cost the index.
    /// </remarks>
    public async Task<Specialty?> GetByNameAsync(string name) =>
        await DbContext.Specialties
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null) =>
        await DbContext.Specialties
            .AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId));

    public async Task<IReadOnlyList<Specialty>> GetAllAsync(bool includeInactive)
    {
        var query = DbContext.Specialties.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.Name).ToListAsync();
    }
}

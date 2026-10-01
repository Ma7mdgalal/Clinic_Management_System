using Clinic.Application.Interfaces.Repository;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext DbContext;

    public GenericRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// Returns null when no row matches. FindAsync also returns an already-tracked
    /// instance without hitting the database, which is what makes read-modify-save work.
    /// </summary>
    public async Task<T?> GetByIdAsync(Guid id) =>
        await DbContext.Set<T>().FindAsync(id);

    /// <summary>
    /// AsNoTracking because callers of a list query only read the results. Skipping the
    /// change tracker avoids building tracking entries for rows nobody will modify.
    /// </summary>
    public async Task<IReadOnlyList<T>> ListAllAsync() =>
        await DbContext.Set<T>().AsNoTracking().ToListAsync();

    public async Task AddAsync(T entity) =>
        await DbContext.Set<T>().AddAsync(entity);

    // Update and Remove are synchronous because they are synchronous: both only mark
    // state on the change tracker. Nothing touches the database until SaveChangesAsync.
    public void Update(T entity) => DbContext.Set<T>().Update(entity);

    public void Remove(T entity) => DbContext.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync() => DbContext.SaveChangesAsync();

    public async Task BeginTransactionAsync()
    {
        await DbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await DbContext.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await DbContext.Database.RollbackTransactionAsync();
    }

}

namespace Clinic.Application.Interfaces.Repository;


public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<T>> ListAllAsync();

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);

    /// Commits everything staged on the shared DbContext. Returns rows affected.
    Task<int> SaveChangesAsync();

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

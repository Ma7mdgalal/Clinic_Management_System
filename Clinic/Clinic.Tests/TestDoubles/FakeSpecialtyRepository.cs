using Clinic.Application.Interfaces.Repository;
using Clinic.Domain.Models;

namespace Clinic.Tests.TestDoubles;

/// <summary>
/// In-memory <see cref="ISpecialtyRepository"/>.
///
/// Hand-written rather than mocked. A mock would let a test pass while asserting nothing
/// about behaviour; this fake actually stores rows, so a test can create a specialty and
/// then read it back, and the duplicate-name rules are exercised against real data.
/// </summary>
public sealed class FakeSpecialtyRepository : ISpecialtyRepository
{
    public FakeSpecialtyRepository(FakeTransaction? transaction = null)
    {
        Transaction = transaction ?? new FakeTransaction();
    }

    public FakeTransaction Transaction { get; }

    /// <summary>The stored rows. Seed directly in a test's arrange step.</summary>
    public List<Specialty> Rows { get; } = [];

    public int SaveChangesCallCount { get; private set; }

    public Task<Specialty?> GetByIdAsync(Guid id) =>
        Task.FromResult(Rows.SingleOrDefault(s => s.Id == id));

    public Task<Specialty?> GetByNameAsync(string name) =>
        Task.FromResult(Rows.SingleOrDefault(
            s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null) =>
        Task.FromResult(Rows.Any(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (excludeId is null || s.Id != excludeId)));

    public Task<IReadOnlyList<Specialty>> GetAllAsync(bool includeInactive)
    {
        IReadOnlyList<Specialty> result = includeInactive
            ? Rows.ToList()
            : Rows.Where(s => s.IsActive).ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Specialty>> ListAllAsync() =>
        Task.FromResult<IReadOnlyList<Specialty>>(Rows.ToList());

    public Task AddAsync(Specialty entity)
    {
        Rows.Add(entity);
        Transaction.OnRollback(() => Rows.Remove(entity));
        return Task.CompletedTask;
    }

    // The fake holds the same object the service mutated, so an update needs no work.
    public void Update(Specialty entity) { }

    public void Remove(Specialty entity) => Rows.Remove(entity);

    public Task<int> SaveChangesAsync()
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync()
    {
        Transaction.Begin();
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync()
    {
        Transaction.Commit();
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync()
    {
        Transaction.Rollback();
        return Task.CompletedTask;
    }
}

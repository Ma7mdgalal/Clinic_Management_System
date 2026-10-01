using Clinic.Application.Interfaces.Repository;
using Clinic.Domain.Models;

namespace Clinic.Tests.TestDoubles;

/// <summary>In-memory <see cref="IPatientRepository"/>. See <see cref="FakeSpecialtyRepository"/>.</summary>
public sealed class FakePatientRepository : IPatientRepository
{
    public FakePatientRepository(FakeTransaction? transaction = null)
    {
        Transaction = transaction ?? new FakeTransaction();
    }

    public FakeTransaction Transaction { get; }

    public List<Patient> Rows { get; } = [];

    public int SaveChangesCallCount { get; private set; }

    /// <summary>See <see cref="FakeDoctorRepository.SaveChangesThrows"/>.</summary>
    public Exception? SaveChangesThrows { get; set; }

    public Task<Patient?> GetByIdAsync(Guid id) =>
        Task.FromResult(Rows.SingleOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Patient>> GetAllAsync(bool includeInactive)
    {
        IReadOnlyList<Patient> result = includeInactive
            ? Rows.ToList()
            : Rows.Where(p => p.IsActive).ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Patient>> ListAllAsync() =>
        Task.FromResult<IReadOnlyList<Patient>>(Rows.ToList());

    public Task AddAsync(Patient entity)
    {
        Rows.Add(entity);
        Transaction.OnRollback(() => Rows.Remove(entity));
        return Task.CompletedTask;
    }

    public void Update(Patient entity) { }

    public void Remove(Patient entity) => Rows.Remove(entity);

    public Task<int> SaveChangesAsync()
    {
        SaveChangesCallCount++;

        if (SaveChangesThrows is not null)
        {
            throw SaveChangesThrows;
        }

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

using Clinic.Application.Interfaces.Repository;
using Clinic.Domain.Models;

namespace Clinic.Tests.TestDoubles;

/// <summary>In-memory <see cref="IDoctorRepository"/>. See <see cref="FakeSpecialtyRepository"/>.</summary>
public sealed class FakeDoctorRepository : IDoctorRepository
{
    public FakeDoctorRepository(FakeTransaction? transaction = null)
    {
        Transaction = transaction ?? new FakeTransaction();
    }

    public FakeTransaction Transaction { get; }

    public List<Doctor> Rows { get; } = [];

    public int SaveChangesCallCount { get; private set; }

    /// <summary>
    /// Set to throw from <see cref="SaveChangesAsync"/>. Simulates a constraint violation
    /// after the Identity account has already been created — the case that proves whether
    /// the service rolls back.
    /// </summary>
    public Exception? SaveChangesThrows { get; set; }

    public Task<Doctor?> GetByIdAsync(Guid id) =>
        Task.FromResult(Rows.SingleOrDefault(d => d.Id == id));

    public Task<Doctor?> GetByLicenseNumberAsync(string licenseNumber) =>
        Task.FromResult(Rows.SingleOrDefault(
            d => string.Equals(d.LicenseNumber, licenseNumber, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Doctor>> GetAllAsync(bool includeInactive)
    {
        IReadOnlyList<Doctor> result = includeInactive
            ? Rows.ToList()
            : Rows.Where(d => d.IsActive).ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Doctor>> ListAllAsync() =>
        Task.FromResult<IReadOnlyList<Doctor>>(Rows.ToList());

    public Task AddAsync(Doctor entity)
    {
        Rows.Add(entity);
        Transaction.OnRollback(() => Rows.Remove(entity));
        return Task.CompletedTask;
    }

    public void Update(Doctor entity) { }

    public void Remove(Doctor entity) => Rows.Remove(entity);

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

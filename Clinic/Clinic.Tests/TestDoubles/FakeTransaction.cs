namespace Clinic.Tests.TestDoubles;

/// <summary>
/// Stands in for the ambient database transaction.
///
/// The production services create an Identity account and a clinic profile inside one
/// transaction, so a failure after the account is created must leave no orphan account
/// behind. To make that assertable, the fake repository and the fake auth service share
/// one instance of this type and register compensating actions on it. A rollback replays
/// those actions in reverse, which is close enough to the real behaviour to catch the bug
/// that matters: a service that forgets to roll back.
/// </summary>
public sealed class FakeTransaction
{
    private readonly List<Action> _compensations = [];

    public bool IsActive { get; private set; }

    public int BeginCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public void Begin()
    {
        IsActive = true;
        BeginCount++;
        _compensations.Clear();
    }

    /// <summary>
    /// Registers how to undo a write. Ignored outside a transaction, which mirrors
    /// auto-commit: a write with no transaction around it cannot be rolled back.
    /// </summary>
    public void OnRollback(Action undo)
    {
        if (IsActive)
        {
            _compensations.Add(undo);
        }
    }

    public void Commit()
    {
        CommitCount++;
        IsActive = false;
        _compensations.Clear();
    }

    public void Rollback()
    {
        RollbackCount++;

        // Reverse order: the last write is undone first, as a real rollback would.
        for (var i = _compensations.Count - 1; i >= 0; i--)
        {
            _compensations[i]();
        }

        _compensations.Clear();
        IsActive = false;
    }
}

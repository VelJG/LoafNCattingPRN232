using LoafNCatting.Application.Interfaces.Common;
using System.Data;

namespace LoafNCatting.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IApplicationDbContext ApplicationDbContext { get; }

    bool IsTransactionActive { get; }

    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers best-effort work that runs only after the current database
    /// transaction has committed successfully. The callback must handle its own
    /// failures and must not be used for database state required by the transaction.
    /// </summary>
    void RegisterAfterCommit(Func<CancellationToken, Task> callback);
}

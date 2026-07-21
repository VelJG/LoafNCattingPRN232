using LoafNCatting.Application.Interfaces.Common;
using System.Data;

namespace LoafNCatting.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IApplicationDbContext ApplicationDbContext { get; }

    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

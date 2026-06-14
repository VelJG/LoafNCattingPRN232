using LoafNCatting.Application.Interfaces.Common;

namespace LoafNCatting.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IApplicationDbContext ApplicationDbContext { get; }

    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}

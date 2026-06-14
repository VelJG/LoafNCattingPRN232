using LoafNCatting.Application.Interfaces.Common;
using LoafNCatting.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    public IApplicationDbContext ApplicationDbContext { get; }

    public UnitOfWork(IApplicationDbContext applicationDbContext)
    {
        ApplicationDbContext = applicationDbContext;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var entityType = typeof(T);

        lock (_repositories)
        {
            if (_repositories.TryGetValue(entityType, out var repository))
            {
                return (IRepository<T>)repository;
            }

            var newRepository = new Repository<T>(ApplicationDbContext);
            _repositories.Add(entityType, newRepository);

            return newRepository;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => ApplicationDbContext.DbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await ApplicationDbContext.DbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await SaveChangesAsync();

        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
    }
}

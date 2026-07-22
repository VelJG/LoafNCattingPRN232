using LoafNCatting.Application.Interfaces.Common;
using LoafNCatting.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

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

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await ApplicationDbContext.DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException &&
                SqlServerErrorClassifier.IsUniqueConstraintViolation(sqlException.Number))
        {
            throw new InvalidOperationException(
                "A record with the same unique value already exists.",
                exception);
        }
    }

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this unit of work.");
        }

        _transaction = await ApplicationDbContext.DbContext.Database.BeginTransactionAsync(
            isolationLevel,
            cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction exists to commit.");
        }

        await SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
        ApplicationDbContext.DbContext.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
    }
}

using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using System.Linq.Expressions;

namespace LoafNCatting.Services.BaseServices;

public abstract class DataServiceBase<TEntity> : IDataService<TEntity> where TEntity : class, new()
{
    protected IUnitOfWork UnitOfWork { get; }

    protected DataServiceBase(IUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public abstract Task AddAsync(TEntity entity);

    public abstract Task DeleteAsync(int? id);

    public abstract Task DeleteAsync(TEntity entity);

    public abstract Task<IList<TEntity>> GetAllAsync();

    public abstract Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);

    public abstract Task<TEntity?> GetOneAsync(int? id);

    public abstract Task UpdateAsync(TEntity entity);
}

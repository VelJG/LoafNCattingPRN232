using LoafNCatting.Application.Interfaces.Common;
using LoafNCatting.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Infrastructure.Repositories;

public sealed class Repository<T> : IRepository<T> where T : class
{
    public IApplicationDbContext ApplicationDbContext { get; }

    public DbSet<T> Entities => ApplicationDbContext.DbContext.Set<T>();

    public Repository(IApplicationDbContext applicationDbContext)
    {
        ApplicationDbContext = applicationDbContext;
    }

    public async Task<IList<T>> GetAllAsync()
        => await Entities.ToListAsync();

    public T? Find(params object[] keyValues)
        => Entities.Find(keyValues);

    public async Task<T?> FindAsync(params object[] keyValues)
        => await Entities.FindAsync(keyValues).AsTask();

    public async Task InsertAsync(T entity, bool saveChanges = true)
    {
        await Entities.AddAsync(entity);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task InsertRangeAsync(IEnumerable<T> entities, bool saveChanges = true)
    {
        await Entities.AddRangeAsync(entities);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(T entity, bool saveChanges = true)
    {
        Entities.Update(entity);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, bool saveChanges = true)
    {
        Entities.UpdateRange(entities);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(T entity, bool saveChanges = true)
    {
        Entities.Remove(entity);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id, bool saveChanges = true)
    {
        var entity = await FindAsync(id);

        if (entity is null)
        {
            throw new KeyNotFoundException($"{typeof(T).Name} with id '{id}' was not found.");
        }

        await DeleteAsync(entity, saveChanges);
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, bool saveChanges = true)
    {
        Entities.RemoveRange(entities);

        if (saveChanges)
        {
            await ApplicationDbContext.DbContext.SaveChangesAsync();
        }
    }
}

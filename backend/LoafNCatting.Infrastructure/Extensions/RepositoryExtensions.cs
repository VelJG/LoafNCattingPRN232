using LoafNCatting.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LoafNCatting.Infrastructure.Extensions;

public static class RepositoryExtensions
{
    public static IQueryable<T> Where<T>(this IRepository<T> repository, Expression<Func<T, bool>> predicate)
        where T : class
        => repository.Entities.Where(predicate);

    public static Task<List<T>> ToListAsync<T>(this IRepository<T> repository)
        where T : class
        => repository.Entities.ToListAsync();

    public static Task<List<T>> ToListAsync<T>(this IRepository<T> repository, Expression<Func<T, bool>> predicate)
        where T : class
        => repository.Entities.Where(predicate).ToListAsync();

    public static IOrderedQueryable<T> OrderBy<T, TKey>(this IRepository<T> repository, Expression<Func<T, TKey>> keySelector)
        where T : class
        => repository.Entities.OrderBy(keySelector);

    public static Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository, Expression<Func<T, bool>> predicate)
        where T : class
        => repository.Entities.FirstOrDefaultAsync(predicate);

    public static Task<bool> AnyAsync<T>(this IRepository<T> repository, Expression<Func<T, bool>> predicate)
        where T : class
        => repository.Entities.AnyAsync(predicate);
}

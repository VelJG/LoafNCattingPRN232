using LoafNCatting.Caching.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Caching.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddCacheServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IDataCached, DataCached>();

        return services;
    }

    public static string GetKey<T>(this IDataCached dataCached, T entity, Func<T, object?> acquire)
        where T : class
    {
        return string.Format(CachingCommonDefaults.CacheKey, typeof(T).Name.ToLower(), acquire(entity));
    }
}

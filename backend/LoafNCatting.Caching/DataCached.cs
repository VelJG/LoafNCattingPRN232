using LoafNCatting.Caching.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace LoafNCatting.Caching;

public class DataCached : IDataCached, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private static readonly ConcurrentDictionary<string, bool> AllKeys = new();
    private CancellationTokenSource _cancellationTokenSource = new();

    public DataCached(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T Get<T>(string key, Func<T> acquire, int? cacheTime = null)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            return value!;
        }

        var result = acquire();
        var effectiveCacheTime = cacheTime ?? CachingCommonDefaults.CacheTime;

        if (effectiveCacheTime > 0)
        {
            Set(key, result, effectiveCacheTime);
        }

        return result;
    }

    public T? Get<T>(string key)
    {
        return _memoryCache.TryGetValue(key, out T? value)
            ? value
            : default;
    }

    public object? Get(string key)
    {
        return _memoryCache.TryGetValue(key, out object? value)
            ? value
            : null;
    }

    public IList<string> GetKeys()
    {
        return AllKeys.Keys.ToList();
    }

    public IList<T> GetValues<T>(string pattern)
    {
        return AllKeys.Keys
            .Where(key => key.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            .Select(Get<T>)
            .Where(value => value is not null)
            .Select(value => value!)
            .ToList();
    }

    public bool IsSet(string key)
    {
        return _memoryCache.TryGetValue(key, out _);
    }

    public void Set<T>(string key, T? value, int cacheTime)
    {
        if (value is null)
        {
            return;
        }

        _memoryCache.Set(AddKey(key), value, GetMemoryCacheEntryOptions(TimeSpan.FromMinutes(cacheTime)));
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        AllKeys.TryRemove(key, out _);
    }

    public void Clear()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        AllKeys.Clear();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(TimeSpan cacheTime)
    {
        return new MemoryCacheEntryOptions()
            .AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token))
            .SetAbsoluteExpiration(cacheTime);
    }

    private static string AddKey(string key)
    {
        AllKeys.TryAdd(key, true);
        return key;
    }
}

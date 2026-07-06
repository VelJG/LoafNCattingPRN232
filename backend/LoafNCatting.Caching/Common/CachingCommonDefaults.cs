namespace LoafNCatting.Caching.Common;

public static class CachingCommonDefaults
{
    public static int CacheTime = 30;

    public static string CacheKey => "loafncatting.{0}.id.{1}";

    public static string AllCacheKey => "loafncatting.{0}.all";

    public static string CacheKeyHeader => "loafncatting.{0}.id";

    public static string UserByIdCacheKey => "loafncatting.user.id.{0}";
}

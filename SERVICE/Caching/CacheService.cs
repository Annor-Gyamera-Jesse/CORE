using Microsoft.Extensions.Caching.Memory;

namespace CORE.SERVICE.Caching
{
    public class CacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        // Generic method for getting or setting cached data
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getDataFunc, int minutes = 15)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                Console.WriteLine($"[CACHE HIT] Key: {key}");
                return cachedValue;
            }

            Console.WriteLine($"[CACHE MISS] Key: {key} - Fetching from database...");

            var result = await getDataFunc();

            _cache.Set(key, result, TimeSpan.FromMinutes(minutes));

            return result;
        }


        // Method to manually remove any cache
        public void Invalidate(string key)
        {
            _cache.Remove(key);
        }
    }
}

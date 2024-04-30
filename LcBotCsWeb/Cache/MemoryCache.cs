namespace LcBotCsWeb.Cache;

public class MemoryCache : ICache
{
    private class MemoryCachedObject
    {
        public DateTime Expires { get; }
        public object Object { get; }

        public MemoryCachedObject(object obj, DateTime expires)
        {
            Object = obj;
            Expires = expires;
        }
    }

    private readonly Dictionary<string, MemoryCachedObject> _cache;

    public MemoryCache()
    {
        _cache = new Dictionary<string, MemoryCachedObject>();
    }

    public Task<bool> Set<T>(string key, T obj, TimeSpan timeToLive) where T : class
        => Task.FromResult(_cache.TryAdd(key, new MemoryCachedObject(obj, DateTime.Now + timeToLive)));

    public Task<bool> Delete(string key)
        => Task.FromResult(_cache.Remove(key));


    public async Task<T?> Get<T>(string key) where T : class
    {
        if (!_cache.TryGetValue(key, out var obj))
            return null;

        if (obj.Expires <= DateTime.Now)
        {
            await Delete(key);
            return null;
        }

        if (obj.Object is T t)
            return t;

        return null;
    }

    public async Task<T> Get<T>(string key, Func<Task<T>> create, TimeSpan timeToLive) where T : class
    {
        var result = await Get<T>(key);

        if (result != null)
            return result;

        var obj = await create();
        await Set(key, obj, timeToLive);
        return obj;
    }

    public Task Clear()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        var toRemove = new List<string>();

        foreach (var (key, value) in _cache)
            if (value.Expires >= DateTime.Now)
                toRemove.Add(key);

        foreach (var key in toRemove)
            _cache.Remove(key);

        return Task.CompletedTask;
    }
}
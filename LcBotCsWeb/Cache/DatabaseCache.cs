using LcBotCsWeb.Database;
using LcBotCsWeb.Database.Repository;

namespace LcBotCsWeb.Cache;

public class DatabaseCache : ICache
{
    private readonly IRepository<DatabaseCachedObject> _collection;

    public DatabaseCache(IDatabase database)
    {
        _collection = database.Cache;
    }

    public async Task<bool> Set(string key, object obj, TimeSpan timeToLive)
    {
        await _collection.Upsert(new DatabaseCachedObject(key, obj, DateTime.Now + timeToLive));
        return true;
    }

    public async Task<bool> Delete(string key)
    {
        var num = await _collection.Delete(item => item.Key == key);
        return num > 0;
    }


    public async Task<T?> Get<T>(string key) where T : class
    {
        var result = (await _collection.Find(item => item.Key == key)).FirstOrDefault();

        if (result == null)
            return null;

        if (result.Expires > DateTime.Now)
            if (result.Object is T t)
                return t;

        await Delete(result.Key);
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

    public async Task Clear()
    {
        await _collection.Delete(_ => true);
    }

    public async Task Cleanup()
    {
        await _collection.Delete(item => item.Expires < DateTime.Now);
    }
}
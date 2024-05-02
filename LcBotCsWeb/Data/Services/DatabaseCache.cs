using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Data.Repositories;

namespace LcBotCsWeb.Data.Services;

public class DatabaseCache : ICache
{
	private readonly Repository<DatabaseObjectWrapper<CachedItem>> _collection;

	public DatabaseCache(Database database)
	{
		_collection = database.Cache;
	}

	public async Task<bool> Create(string key, object obj, TimeSpan timeToLive)
	{
		await _collection.Upsert(new DatabaseObjectWrapper<CachedItem>() { Data = new CachedItem(key, obj, DateTime.UtcNow + timeToLive) });
		return true;
	}

	public async Task<bool> Delete(string key)
	{
		var num = await _collection.Delete(item => item.Data.Key == key);
		return num > 0;
	}

	public async Task<T?> Get<T>(string key) where T : class
	{
		var result = (await _collection.Find(item => item.Data.Key == key)).FirstOrDefault();

		if (result == null)
			return null;

		if (result.Data.Expires > DateTime.UtcNow)
			if (result.Data.Object is T t)
				return t;

		await Delete(result.Data.Key);
		return null;
	}

	public async Task<T> GetOrCreate<T>(string key, Func<Task<T>> create, TimeSpan timeToLive) where T : class
	{
		var result = await Get<T>(key);

		if (result != null)
			return result;

		var obj = await create();
		await Create(key, obj, timeToLive);
		return obj;
	}

	public async Task Clear()
	{
		await _collection.Delete(_ => true);
	}

	public async Task Cleanup()
	{
		await _collection.Delete(item => item.Data.Expires < DateTime.UtcNow);
	}
}
using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Data.Repositories;
using MongoDB.Driver.Linq;

namespace LcBotCsWeb.Data.Services;

public class DatabaseCache : ICache
{
	private readonly Repository<CachedItem>? _collection;

	public DatabaseCache(Database database)
	{
		_collection = database.Cache;
	}

	public async Task<bool> Create(string key, object? obj, TimeSpan timeToLive)
	{
		if (_collection == null)
			return false;

		if (obj == null)
			return false;

		await _collection.Upsert(new CachedItem(key, obj, DateTime.UtcNow + timeToLive));
		return true;
	}

	public async Task<bool> Delete(string key)
	{
		if (_collection == null)
			return false;

		var num = await _collection.Delete(item => item.Key == key);
		return num > 0;
	}

	public async Task<T?> Get<T>(string key) where T : class
	{
		if (_collection == null)
			return null;

		var result = await _collection.Query.FirstOrDefaultAsync(item => item.Key == key);

		if (result == null)
			return null;

		if (result.Expires > DateTime.UtcNow)
			if (result.Object is T t)
				return t;

		await Delete(result.Key);
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
		if (_collection == null)
			return;

		await _collection.Delete(_ => true);
	}

	public async Task Cleanup()
	{
		if (_collection == null)
			return;

		await _collection.Delete(item => item.Expires < DateTime.UtcNow);
	}
}
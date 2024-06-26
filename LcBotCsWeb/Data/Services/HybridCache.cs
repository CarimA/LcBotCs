using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Repositories;

namespace LcBotCsWeb.Data.Services;

public class HybridCache : ICache
{
	private readonly MemoryCache _memoryCache;
	private readonly DatabaseCache _databaseCache;

	public HybridCache(Database database)
	{
		_memoryCache = new MemoryCache();
		_databaseCache = new DatabaseCache(database);
	}

	public async Task<bool> Create(string key, object obj, TimeSpan timeToLive)
	{
		var results = await Task.WhenAll(
			_memoryCache.Create(key, obj, timeToLive),
			_databaseCache.Create(key, obj, timeToLive));

		return results.Any(result => result);
	}

	public async Task<bool> Delete(string key)
	{
		var results = await Task.WhenAll(
			_memoryCache.Delete(key),
			_databaseCache.Delete(key));

		return results.Any(result => result);
	}

	public async Task<T?> Get<T>(string key) where T : class
	{
		return await _memoryCache.Get<T>(key) ?? await _databaseCache.Get<T>(key);
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
		await Task.WhenAll(_memoryCache.Clear(), _databaseCache.Clear());
	}

	public async Task Cleanup()
	{
		await Task.WhenAll(_memoryCache.Cleanup(), _databaseCache.Cleanup());
	}
}
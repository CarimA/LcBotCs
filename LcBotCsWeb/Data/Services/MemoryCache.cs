using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Models;

namespace LcBotCsWeb.Data.Services;

public class MemoryCache : ICache
{
	private readonly Dictionary<string, CachedItem> _cache;

	public MemoryCache()
	{
		_cache = new Dictionary<string, CachedItem>();
	}

	public Task<bool> Create(string key, object obj, TimeSpan timeToLive)
		=> Task.FromResult(_cache.TryAdd(key, new CachedItem(key, obj, DateTime.UtcNow + timeToLive)));

	public Task<bool> Delete(string key)
		=> Task.FromResult(_cache.Remove(key));


	public async Task<T?> Get<T>(string key) where T : class
	{
		if (!_cache.TryGetValue(key, out var obj))
			return null;

		if (obj.Expires <= DateTime.UtcNow)
		{
			await Delete(key);
			return null;
		}

		if (obj.Object is T t)
			return t;

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

	public Task Clear()
	{
		_cache.Clear();
		return Task.CompletedTask;
	}

	public Task Cleanup()
	{
		var toRemove = new List<string>();

		foreach (var (key, value) in _cache)
			if (value.Expires < DateTime.UtcNow)
				toRemove.Add(key);

		foreach (var key in toRemove)
			_cache.Remove(key);

		return Task.CompletedTask;
	}
}
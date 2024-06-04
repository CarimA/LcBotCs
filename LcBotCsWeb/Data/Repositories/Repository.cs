using LcBotCsWeb.Data.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace LcBotCsWeb.Data.Repositories;

public class Repository<T> where T : DatabaseObject
{
	private readonly IMongoCollection<T> _collection;

	public Repository(IMongoCollection<T> collection)
	{
		_collection = collection;
	}

	public async Task Insert(T item)
	{
		await _collection.InsertOneAsync(item);
	}

	public async Task<bool> Delete(T item)
	{
		var result = await _collection.DeleteOneAsync(MatchById(item));
		return result.DeletedCount == 1;
	}

	public async Task<int> Delete(Expression<Func<T, bool>> predicate)
	{
		var result = await _collection.DeleteManyAsync(predicate);
		return (int)result.DeletedCount;
	}

	public async Task Update(T item)
	{
		item.DateModified = DateTime.UtcNow;
		await _collection.ReplaceOneAsync(MatchById(item), item);
	}

	public async Task Upsert(T item)
	{
		item.DateModified = DateTime.UtcNow;
		await _collection.ReplaceOneAsync(MatchById(item), item, new ReplaceOptions() { IsUpsert = true });
	}

	public IAsyncEnumerable<T> FindAll()
	{
		return _collection.Find(_ => true).ToAsyncEnumerable();
	}

	public async Task<T> FindOne(string id)
	{
		var result = await _collection.FindAsync(item => item.Id == ObjectId.Parse(id));
		return await result.FirstOrDefaultAsync();
	}

	public Task<T> FindOne(int id)
	{
		throw new NotSupportedException();
	}

	public async Task<List<T>> Find(Expression<Func<T, bool>> predicate)
	{
		return await _collection.FindAsync(predicate).Result.ToListAsync();
	}

	private static FilterDefinition<T>? MatchById(T item)
	{
		return Builders<T>.Filter.Eq(r => r.Id, item.Id);
	}

	public IMongoQueryable<T> Query => _collection.AsQueryable();
}
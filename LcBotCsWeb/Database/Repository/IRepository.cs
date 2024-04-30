using System.Linq.Expressions;

namespace LcBotCsWeb.Database.Repository;

public interface IRepository<T> where T : DatabaseObject
{
    Task Insert(T item);
    Task<bool> Delete(T item);
    Task<int> Delete(Expression<Func<T, bool>> predicate);
    Task Update(T item);
    Task Upsert(T item);

    IAsyncEnumerable<T> FindAll();
    Task<T> FindOne(string id);
    Task<List<T>> Find(Expression<Func<T, bool>> predicate);

    IQueryable<T> Query { get; }
}
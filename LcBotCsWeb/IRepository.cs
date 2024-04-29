namespace LCBotCs;

public interface IRepository<T>
{
    Task Insert(T item);
    Task Delete(T item);
    Task Update(T item);

    IAsyncEnumerable<T> FindAll();
    Task<T> FindOne(string guid);

    IQueryable<T> Query { get; }
}
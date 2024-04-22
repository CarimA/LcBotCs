namespace LCBotCs;
public interface ICache
{
    Task<bool> Set<T>(string key, T obj, TimeSpan timeToLive) where T : class;
    Task<bool> Delete(string key);
    Task<T?> Get<T>(string key) where T : class;
    Task<T> Get<T>(string key, Func<Task<T>> create, TimeSpan timeToLive) where T : class;
    Task Clear();
    Task Cleanup();
}
namespace LcBotCsWeb.Data.Interfaces;

public interface ICache
{
    Task<bool> Set(string key, object obj, TimeSpan timeToLive);
    Task<bool> Delete(string key);
    Task<T?> Get<T>(string key) where T : class;
    Task<T> Get<T>(string key, Func<Task<T>> create, TimeSpan timeToLive) where T : class;
    Task Clear();
    Task Cleanup();
}
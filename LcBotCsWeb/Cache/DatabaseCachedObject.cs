using LcBotCsWeb.Database;

namespace LcBotCsWeb.Cache;

public class DatabaseCachedObject : DatabaseObject, ICachedObject
{
    public string Key { get; }
    public DateTime Expires { get; }
    public object Object { get; }

    public DatabaseCachedObject(string key, object obj, DateTime expires)
    {
        Key = key;
        Object = obj;
        Expires = expires;
    }
}
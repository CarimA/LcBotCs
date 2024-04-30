namespace LcBotCsWeb.Data.Models;

public class CachedItem
{
    public string Key { get; set; }
    public DateTime Expires { get; set; }
    public object Object { get; set; }

    public CachedItem(string key, object obj, DateTime expires)
    {
        Key = key;
        Object = obj;
        Expires = expires;
    }
}
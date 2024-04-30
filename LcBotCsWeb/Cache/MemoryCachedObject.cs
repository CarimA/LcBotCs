namespace LcBotCsWeb.Cache;

public class MemoryCachedObject : ICachedObject
{
    public DateTime Expires { get; }
    public object Object { get; }

    public MemoryCachedObject(object obj, DateTime expires)
    {
        Object = obj;
        Expires = expires;
    }
}
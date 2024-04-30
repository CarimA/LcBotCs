namespace LcBotCsWeb.Cache;

public interface ICachedObject
{
    public DateTime Expires { get; }
    public object Object { get; }
}
namespace LcBotCsWeb.Data.Models;

public class DatabaseOptions
{
    public string ConnectionString { get; set; }

    public string DatabaseName { get; set; }
    public string? CacheCollectionName { get; set; }
}
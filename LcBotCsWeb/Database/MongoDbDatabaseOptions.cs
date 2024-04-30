namespace LcBotCsWeb.Database;

public class MongoDbDatabaseOptions
{
    public string ConnectionString { get; set; }

    public string DatabaseName { get; set; }
    public string? CacheCollectionName { get; set; }
}
using LcBotCsWeb.Cache;
using LcBotCsWeb.Database.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace LcBotCsWeb.Database;

public class MongoDbDatabase : IDatabase
{
    public IRepository<DatabaseCachedObject>? Cache { get; }

    public MongoDbDatabase(MongoDbDatabaseOptions options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        var client = new MongoClient(settings);

        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Debug.Print("Successfully connected to Mongodb");

            var database = client.GetDatabase(options.DatabaseName);

            if (!string.IsNullOrWhiteSpace(options.CacheCollectionName))
            {
                Cache = new MongoDbRepository<DatabaseCachedObject>(
                    database.GetCollection<DatabaseCachedObject>(options.CacheCollectionName));
            }
        }
        catch (Exception ex)
        {
            Debug.Print(ex.Message);
            throw;
        }
    }
}
using LcBotCsWeb.Data.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Diagnostics;

namespace LcBotCsWeb.Data.Repositories;

public class Database
{
	public Repository<DatabaseObjectWrapper<CachedItem>>? Cache { get; }

	public Database(DatabaseOptions options)
	{
		var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
		settings.ServerApi = new ServerApi(ServerApiVersion.V1);
		settings.LinqProvider = LinqProvider.V3;

		var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
		BsonSerializer.RegisterSerializer(objectSerializer);

		var client = new MongoClient(settings);

		try
		{
			var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
			Debug.Print("Successfully connected to Mongodb");

			var database = client.GetDatabase(options.DatabaseName);

			if (!string.IsNullOrWhiteSpace(options.CacheCollectionName))
			{
				Cache = new Repository<DatabaseObjectWrapper<CachedItem>>(database.GetCollection<DatabaseObjectWrapper<CachedItem>>(options.CacheCollectionName));
			}
		}
		catch (Exception ex)
		{
			Debug.Print(ex.Message);
			throw;
		}
	}
}
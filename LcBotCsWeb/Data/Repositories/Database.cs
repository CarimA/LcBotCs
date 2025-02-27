using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Misc;
using LcBotCsWeb.Modules.PsimDiscordLink;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LcBotCsWeb.Data.Repositories;

public class Database
{
	private readonly IMongoDatabase _database;
	public Repository<CachedItem>? Cache { get; }
	public Repository<AccountLinkItem> AccountLinks { get; }
	public Repository<VerificationCodeItem> VerificationCodes { get; }
	public Repository<PsimAlt> Alts { get; }
	public Repository<BridgeWebhook> BridgeWebhooks { get; }
	public Repository<BridgeMessage> BridgeMessages { get; }

	public Database(Configuration config)
	{
		var settings = MongoClientSettings.FromConnectionString(config.DatabaseConnectionString);
		settings.ServerApi = new ServerApi(ServerApiVersion.V1);
		settings.LinqProvider = LinqProvider.V3;

		var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
		BsonSerializer.RegisterSerializer(objectSerializer);

		var client = new MongoClient(settings);

		try
		{
			var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
			Console.WriteLine("Successfully connected to Mongodb");

			_database = client.GetDatabase(config.DatabaseName);

			if (!string.IsNullOrWhiteSpace(config.DatabaseCacheCollectionName))
			{
				Cache = GetCollection<CachedItem>(config.DatabaseCacheCollectionName);
			}

			AccountLinks = GetCollection<AccountLinkItem>("account-link");
			VerificationCodes = GetCollection<VerificationCodeItem>("verification-codes");
			Alts = GetCollection<PsimAlt>("alts");
			BridgeWebhooks = GetCollection<BridgeWebhook>("bridge-webhooks");
			BridgeMessages = GetCollection<BridgeMessage>("bridge-messages");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			throw;
		}
	}

	private Repository<T> GetCollection<T>(string collection) where T : DatabaseObject
	{
		return new Repository<T>(_database.GetCollection<T>(collection));
	}
}
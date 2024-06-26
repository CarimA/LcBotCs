﻿using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Modules.AltTracking;
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
			Console.WriteLine("Successfully connected to Mongodb");

			_database = client.GetDatabase(options.DatabaseName);

			if (!string.IsNullOrWhiteSpace(options.CacheCollectionName))
			{
				Cache = GetCollection<CachedItem>(options.CacheCollectionName);
			}

			AccountLinks = GetCollection<AccountLinkItem>("account-link");
			VerificationCodes = GetCollection<VerificationCodeItem>("verification-codes");
			Alts = GetCollection<PsimAlt>("alts");
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
using LcBotCsWeb.Data.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.AltTracking;

public class AltTrackingService : ISubscriber<RoomUsers>, ISubscriber<UserJoinRoom>, ISubscriber<UserRename>
{
    private readonly Database _database;

    public AltTrackingService(Database database)
    {
        _database = database;
    }

    public async Task HandleEvent(RoomUsers e)
    {
        await Task.WhenAll(e.Users.Select(TryAddUser));
    }

    public async Task HandleEvent(UserJoinRoom e)
    {
        await TryAddUser(e.User);
    }

    public async Task HandleEvent(UserRename e)
    {
        await TryAddAlt(e.OldId, e.User);
    }

    private async Task TryAddUser(PsimUsername username)
    {
        var records = await _database.Alts.Find(alts => alts.PsimId == username.Token);

        if (records.Count > 0)
            return;

        await _database.Alts.Insert(new AltRecord() { PsimId = username.Token, AltId = ObjectId.GenerateNewId() });
    }

    private async Task TryAddAlt(string id, PsimUsername alt)
    {
        var results = await _database.Alts.Query
            .Where(alts => alts.PsimId == id || alts.PsimId == alt.Token)
            .ToListAsync();

        if (results == null || results.Count == 0)
            return;

        var firstId = results.First().AltId;

        foreach (var result in results)
	        result.AltId = firstId;

        if (results.All(result => result.PsimId != id))
			results.Add(new AltRecord() { PsimId = id, AltId = firstId });

        if (results.All(result => result.PsimId != alt.Token))
	        results.Add(new AltRecord() { PsimId = alt.Token, AltId = firstId });

		await Task.WhenAll(results.Select(_database.Alts.Upsert));
    }

    public async Task<List<string>> GetAlts(string id)
	{
		var results = await _database.Alts.Query
			.Where(alts => alts.PsimId == id)
			.ToListAsync();

		if (results == null || results.Count == 0)
			return new List<string>();

		var firstId = results.First().AltId;

		var final = await _database.Alts.Find(result => result.AltId == firstId);
		return final.Select(result => result.PsimId).ToList();
    }
}
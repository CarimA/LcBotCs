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
        var records = await _database.Alts.Find(alts => alts.Alts.Any(alt => alt.PsimId == username.Token));

        if (records.Count > 0)
            return;

        var alt = new PsimAlt() { PsimId = username.Token, PsimDisplayName = username.DisplayName };
        var psimUser = new PsimUserItem() { Active = alt, Alts = new List<PsimAlt>() { alt }, AltId = ObjectId.GenerateNewId() };

        await _database.Alts.Insert(psimUser);
    }

    private async Task TryAddAlt(string id, PsimUsername username)
    {
        var results = await _database.Alts.Query
            .Where(alts => alts.Alts.Any(a => a.PsimId == id) || alts.Alts.Any(a => a.PsimId == username.Token))
            .ToListAsync();

        if (results == null || results.Count == 0)
            return;
        
        await MergeUser(results, username);
    }

    private async Task<PsimUserItem?> MergeUser(List<PsimUserItem> users, PsimUsername username)
    {
	    switch (users.Count)
	    {
		    case 0:
			    return null;
	    }

	    var firstId = users.First().AltId;
	    var active = new PsimAlt() { PsimId = username.Token, PsimDisplayName = username.DisplayName };
	    var alts = users.SelectMany(result => result.Alts).ToList();
	    alts.Add(active);

	    var psimUser = new PsimUserItem() { Active = active, Alts = alts.Distinct().ToList(), AltId = firstId };

	    foreach (var result in users)
		    await _database.Alts.Delete(result);

	    await _database.Alts.Insert(psimUser);
        return psimUser;
	}

    public async Task<PsimUserItem?> GetUser(ObjectId id)
    {
	    return await _database.Alts.Query.FirstOrDefaultAsync(alts => alts.AltId == id);
    }

	public async Task<PsimUserItem?> GetUser(PsimUsername username)
	{
		var results = await _database.Alts.Query
			.Where(alts => alts.Alts.Any(alt => alt.PsimId == username.Token))
			.ToListAsync();

		if (results == null || results.Count == 0)
			return null;

		var user = await MergeUser(results, username);
		return user;
	}
}
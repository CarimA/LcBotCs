using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.PsimDiscordLink;
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
	private HashSet<string> _renameCache;

	public AltTrackingService(Database database)
	{
		_database = database;
		_renameCache = new HashSet<string>();
	}

	public async Task HandleEvent(RoomUsers e)
	{
		foreach (var user in e.Users)
			await TryAddUser(user);
	}

	public async Task HandleEvent(UserJoinRoom e)
	{
		await TryAddUser(e.User);
	}

	public async Task HandleEvent(UserRename e)
	{
		// prevent potential duplicate attempts
		if (_renameCache.Contains(e.OldId))
			return;

		_renameCache.Add(e.OldId);
		await TryAddUser(e.User);
		await TryAddAlt(e.OldId, e.User);
		_renameCache.Remove(e.OldId);
	}

	private async Task<bool> TryAddUser(PsimUsername username)
	{
		var alts = await _database.Alts.Query.Where(alt => alt.PsimId == username.Token).ToListAsync();

		if (alts.Count > 0)
		{
			await UpdateNameCasing(alts, username);
			return false;
		}

		var id = ObjectId.GenerateNewId();
		var alt = new PsimAlt() { PsimId = username.Token, PsimDisplayName = username.DisplayName, IsActive = true, AltId = id };
		await _database.Alts.Insert(alt);

		Console.WriteLine($"New user {alt.PsimId} added ({id})");
		return true;
	}

	private async Task<(List<PsimAlt>? Alts, AccountLinkItem? AccountLink, PsimAlt? ActiveUser)> GetAccount(List<PsimAlt>? alts)
	{
		var psimUserId = alts?.FirstOrDefault()?.AltId;
		var activeUser = alts?.FirstOrDefault(psimAlt => psimAlt.IsActive);

		if (psimUserId == null)
			return (alts, null, activeUser);

		// only show a linked username if they're using the alt that's set to display
		var accountLink = await _database.AccountLinks.Query.FirstOrDefaultAsync(link => link.PsimUser == psimUserId);
		return (alts, accountLink, activeUser);
	}

	public async Task<(List<PsimAlt>? Alts, AccountLinkItem? AccountLink, PsimAlt? ActiveUser)> GetAccountByUsername(PsimUsername user)
	{
		var alts = await _database.Alts.Query
			.Where(alt => alt.PsimId == user.Token)
			.ToListAsync();

		return await GetAccount(alts);
	}

	public async Task<(List<PsimAlt>? Alts, AccountLinkItem? AccountLink, PsimAlt? ActiveUser)> GetAccountByDatabaseEntry(ObjectId id)
	{
		var alts = await _database.Alts.Query
			.Where(alts => alts.AltId == id)
			.ToListAsync();

		return await GetAccount(alts);
	}

	public async Task<(List<PsimAlt>? Alts, AccountLinkItem? AccountLink, PsimAlt? ActiveUser)> GetAccountByDiscordId(ulong discordId)
	{
		var link = await _database.AccountLinks.Query
			.FirstOrDefaultAsync(link => link.DiscordId == discordId);

		if (link != null)
		{
			var id = link.PsimUser;
			var alts = await _database.Alts.Query
				.Where(alt => alt.AltId == id)
				.ToListAsync();

			return (alts, link, alts?.FirstOrDefault(psimAlt => psimAlt.IsActive));
		}

		return (null, link, null);
	}


	private async Task UpdateNameCasing(List<PsimAlt> alts, PsimUsername username)
	{
		foreach (var alt in alts.Where(alt => alt.PsimDisplayName != username.DisplayName))
		{
			Console.WriteLine($"Updated name casing {alt.PsimDisplayName} -> {username.DisplayName}");
			alt.PsimDisplayName = username.DisplayName;
			await _database.Alts.Update(alt);
		}
	}

	private async Task TryAddAlt(string id, PsimUsername username)
	{
		/*
		 * 1) get users that match tokens for the old name and new name
		 * 2) ensure they all have the same alt id
		 * 3) set is active for the non-used ones to false, set is active for the current one to true
		 */

		var matchingToken = await _database.Alts.Query.Where(alt => alt.PsimId == username.Token || alt.PsimId == id).ToListAsync();

		if (matchingToken.Count == 0)
			return;

		var altIds = matchingToken.Select(alt => alt.AltId).ToList();

		if (!altIds.Any())
			return;

		var matchingUsers = await _database.Alts.Query.Where(alt => altIds.Contains(alt.AltId)).ToListAsync();

		if (matchingUsers.Count == 0)
			return;

		var firstId = altIds.FirstOrDefault();

		var didSomethingChange = false;
		var discordIds = new HashSet<ulong>();
		var todo = new List<Task>();

		foreach (var user in matchingUsers)
		{
			if (user.IsActive == (user.PsimId == username.Token) && user.AltId == firstId)
				continue;

			var accountLink = await _database.AccountLinks.Query.Where(link => link.PsimUser == user.AltId)
				.ToListAsync();

			didSomethingChange = true;
			user.AltId = firstId;

			foreach (var link in accountLink)
			{
				link.PsimUser = firstId;
				todo.Add(_database.AccountLinks.Update(link));
				discordIds.Add(link.DiscordId);
			}

			todo.Add(_database.Alts.Update(user));
		}

		if (discordIds.Count > 1)
		{
			var ids = discordIds.Select(discordId => $"<@{discordId}>");
			Console.WriteLine($"WARNING: attempted to deduplicate the following alt accounts but they appear to belong to multiple unique users, indicating the possibility of account sharing, please inspect: {string.Join(", ", ids)}");
			return;
		}

		await Task.WhenAll(todo);

		if (didSomethingChange)
			Console.WriteLine($"Alts updated ({firstId}) - active: {username.Token} - {string.Join(", ", matchingUsers.Select(user => user.PsimId))}");
	}

	public async Task UpdateActiveUser(AccountLinkItem user, PsimAlt chosen)
	{
		var links = await _database.AccountLinks.Query
			.Where(link => link.Id == user.Id)
			.ToListAsync();

		foreach (var link in links)
		{
			var id = link.PsimUser;
			var alts = await _database.Alts.Query
				.Where(alt => alt.AltId == id)
				.ToListAsync();

			foreach (var alt in alts)
			{
				alt.IsActive = alt.Id == chosen.Id;
				await _database.Alts.Update(alt);
			}
		}
	}
}
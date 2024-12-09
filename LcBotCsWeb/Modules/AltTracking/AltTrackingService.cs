using Discord;
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

		var altIds = matchingToken.Select(alt => alt.AltId);

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
			var oldActive = user.IsActive;
			var newActive = user.PsimId == username.Token;

			var oldId = user.AltId;

			var changed = (oldActive != newActive) || (oldId != firstId);

			if (changed)
			{
				var accountLink = await _database.AccountLinks.Query.Where(link => link.PsimUser == user.AltId)
					.ToListAsync();

				didSomethingChange = true;
				user.IsActive = newActive;
				user.AltId = firstId;

				foreach (var link in accountLink)
				{
					link.PsimUser = firstId;
					todo.Add(_database.AccountLinks.Update(link));
					discordIds.Add(link.DiscordId);
				}

				todo.Add(_database.Alts.Update(user));
			}
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

	public async Task<List<PsimAlt>?> GetUser(ObjectId id)
	{
		return await _database.Alts.Query
			.Where(alts => alts.AltId == id)
			.ToListAsync();
	}

	public async Task<List<PsimAlt>?> GetUser(PsimUsername username)
	{
		return await _database.Alts.Query
			.Where(alt => alt.PsimId == username.Token)
			.ToListAsync();
	}

	public async Task<List<PsimAlt>?> GetUser(ulong discordId)
	{
		var links = await _database.AccountLinks.Query
			.Where(link => link.DiscordId == discordId)
			.ToListAsync();

		var output = new List<PsimAlt>();
		foreach (var link in links)
		{
			var id = link.PsimUser;
			var alts = await _database.Alts.Query
				.Where(alt => alt.AltId == id)
				.ToListAsync();
			output.AddRange(alts);
		}
		return output;
	}

	public async Task<PsimAlt?> GetActiveUser(ObjectId id)
	{
		return (await GetUser(id))?.FirstOrDefault(user => user.IsActive);
	}

	public async Task<PsimAlt?> GetActiveUser(PsimUsername username)
	{
		return (await GetUser(username))?.FirstOrDefault(user => user.IsActive);
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
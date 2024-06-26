using LcBotCsWeb.Data.Repositories;
using MongoDB.Bson;
using PsimCsLib.Entities;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.AltTracking;

public class PunishmentTrackingService : ISubscriber<UserLocked>, ISubscriber<UserMuted>, ISubscriber<UserUnlocked>,
	ISubscriber<UserUnmuted>, ISubscriber<UserBanned>, ISubscriber<UserUnbanned>
{
	private readonly AltTrackingService _altTracking;
	private readonly Database _database;

	public PunishmentTrackingService(AltTrackingService altTracking, Database database)
	{
		_altTracking = altTracking;
		_database = database;
	}

	private async Task SetPunishment(PsimUsername username, DateTime? date)
	{
		var users = await _altTracking.GetUser(username);

		if (users == null)
			return;

		foreach (var user in users)
		{
			user.ActivePunishment = DateTime.UtcNow + TimeSpan.FromDays(30);
			await _database.Alts.Update(user);
		}
	}

	private async Task ApplyPunishment(PsimUsername username, TimeSpan length)
	{
		await SetPunishment(username, DateTime.UtcNow + length);
	}

	private async Task RemovePunishment(PsimUsername username)
	{
		await SetPunishment(username, null);
	}

	public async Task<bool> IsUserPunished(PsimUsername username)
	{
		var users = await _altTracking.GetUser(username);
		return users != null && users.Any(user => user.ActivePunishment > DateTime.UtcNow);
	}

	public async Task<bool> IsUserPunished(ObjectId id)
	{
		var users = await _altTracking.GetUser(id);
		return users != null && users.Any(user => user.ActivePunishment > DateTime.UtcNow);
	}

	public async Task HandleEvent(UserLocked e)
	{
		if (!e.IsIntro)
			return;

		await ApplyPunishment(e.User, TimeSpan.FromDays(30));
	}

	public async Task HandleEvent(UserMuted e)
	{
		if (!e.IsIntro)
			return;

		await ApplyPunishment(e.User, TimeSpan.FromHours(1));
	}

	public async Task HandleEvent(UserBanned e)
	{
		if (!e.IsIntro)
			return;

		await ApplyPunishment(e.User, TimeSpan.FromDays(3));
	}

	public async Task HandleEvent(UserUnlocked e)
	{
		if (!e.IsIntro)
			return;

		await RemovePunishment(e.User);
	}

	public async Task HandleEvent(UserUnmuted e)
	{
		if (!e.IsIntro)
			return;

		await RemovePunishment(e.User);
	}

	public async Task HandleEvent(UserUnbanned e)
	{
		if (!e.IsIntro)
			return;

		await RemovePunishment(e.User);
	}
}
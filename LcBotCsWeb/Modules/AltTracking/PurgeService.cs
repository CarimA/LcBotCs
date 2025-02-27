using LcBotCsWeb.Data.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LcBotCsWeb.Modules.AltTracking;

public class PurgeService
{
	private readonly DiscordBotService _discord;
	private readonly Configuration _config;
	private readonly AltTrackingService _altTracking;
	private readonly PsimBotService _psim;
	private readonly Database _database;

	public const string PurgeReason = "(this bridged message was purged by a moderator)";

	public PurgeService(DiscordBotService discord, Configuration config, AltTrackingService altTracking, PsimBotService psim,
		Database database)
	{
		_discord = discord;
		_config = config;
		_altTracking = altTracking;
		_psim = psim;
		_database = database;
	}

	public async Task Purge(string username, string room, bool ban = false)
	{
		var configs = _config.BridgedGuilds.Where(linkedGuild => linkedGuild.PsimRoom == room);

		foreach (var config in configs)
		{
			await Purge(username, room, config, ban);
		}
	}

	private async Task Purge(string username, string room, PsimLinkedGuild guildConfig, bool ban = false)
	{
		var guild = _discord.Client.GetGuild(guildConfig.GuildId);
		var channel = guild?.GetTextChannel(guildConfig.BridgeRoom);

		if (channel == null)
			return;

		var (_, accountLink, _) = await _altTracking.GetAccountByUsername(username);

		if (accountLink == null)
			return;

		var after = DateTime.UtcNow - TimeSpan.FromDays(7);
		var userId = accountLink.DiscordId;

		var messages = await _database.BridgeMessages.Query
			.Where(message => message.DateCreated >= after)
			.Where(message => message.DiscordId == userId)
			.Where(message => message.ChannelId == channel.Id)
			.ToListAsync();

		foreach (var message in messages)
		{
			await _psim.Client.Rooms[room].ChangeHtml(message.HtmlId, PurgeReason);
		}

		if (ban)
		{
			var user = guild?.GetUser(userId);
			if (user != null)
				await user.AddRoleAsync(guildConfig.BlockedRoleId);
		}
	}
}
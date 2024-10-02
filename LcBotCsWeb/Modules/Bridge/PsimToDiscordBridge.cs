using Discord;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Bridge;

public class PsimToDiscordBridge : ISubscriber<ChatMessage>
{
	private readonly DiscordBotService _discord;
	private readonly Configuration _config;
	private readonly AltTrackingService _altTracking;
	private readonly Database _db;

	public PsimToDiscordBridge(DiscordBotService discord, Configuration config, AltTrackingService altTracking, Database db)
	{
		_discord = discord;
		_config = config;
		_altTracking = altTracking;
		_db = db;
	}

	public async Task HandleEvent(ChatMessage msg)
	{
		if (msg.User.DisplayName == Utils.GetEnvVar("PSIM_USERNAME", "PSIM_USERNAME"))
			return;

		if (msg.IsIntro)
			return;

		var message = msg.Message.Trim();

		if (message.StartsWith('/') || message.StartsWith('!'))
			return;

		var config = _config.BridgedGuilds.FirstOrDefault(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase));

		if (config == null)
			return;

		var isMultiRoom = _config.BridgedGuilds.Count(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase)) > 1;
		var name = $"{PsimUsername.FromRank(msg.User.Rank)}{msg.User.DisplayName}".Trim();
		var psimUserId = (await _altTracking.GetUser(msg.User))?.FirstOrDefault().AltId;
		var discordId = string.Empty;

		if (psimUserId != null)
		{
			var accountLink = await _db.AccountLinks.Query.FirstOrDefaultAsync(link => link.PsimUser == psimUserId);
			if (accountLink != null)
			{
				discordId = $"{accountLink.DiscordId}";
			}
		}

		var displayRoom = isMultiRoom ? $"[{msg.Room.Name}] " : string.Empty;
		var displayName = string.IsNullOrEmpty(discordId) ? name : $"<@{discordId}> - {name}";

		var output = $"-# **{displayRoom}{displayName}**\n{message}";

		if (string.IsNullOrEmpty(output))
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is not ITextChannel channel)
			return;

		await channel.SendMessageAsync(output, allowedMentions: AllowedMentions.None);
	}
}
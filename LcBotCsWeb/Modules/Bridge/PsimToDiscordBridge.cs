using System.Runtime.InteropServices.ComTypes;
using Discord;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Bridge;

public class PsimToDiscordBridge : ISubscriber<ChatMessage>
{
	private readonly DiscordBotService _discord;
	private readonly BridgeOptions _bridgeOptions;
	private readonly AltTrackingService _altTracking;
	private readonly Database _db;

	public PsimToDiscordBridge(DiscordBotService discord, BridgeOptions bridgeOptions, AltTrackingService altTracking, Database db)
	{
		_discord = discord;
		_bridgeOptions = bridgeOptions;
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

		var config = _bridgeOptions.LinkedGuilds.FirstOrDefault(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase));

		if (config == null)
			return;

		var isMultiRoom = _bridgeOptions.LinkedGuilds.Count(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase)) > 1;
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
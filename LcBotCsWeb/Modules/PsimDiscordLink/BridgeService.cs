using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Ganss.Xss;
using LcBotCsWeb.Data.Repositories;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Enums;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

/*public class ApplyPunishmentsService : ISubscriber<ChatMessage>
{
	public Task HandleEvent(ChatMessage e)
	{

	}
}

public class AltTrackingService : ISubscriber<RoomUsers>, ISubscriber<>*/

public class Alts
{
	public HashSet<string> PsimIds { get; set; }
}


public class ActivePunishment
{
	public string PsimId { get; set; }
	public DateTime Expires { get; set; }
}

public class BridgeService : ISubscriber<ChatMessage>
{
	private readonly Database _database;
	private readonly DiscordBotService _discord;
	private readonly BridgeOptions _bridgeOptions;
	private readonly PsimBotService _psim;
	private readonly HtmlSanitizer _sanitiser;

	public BridgeService(Database database, DiscordBotService discord, BridgeOptions bridgeOptions, PsimBotService psim)
	{
		_database = database;
		_discord = discord;
		_bridgeOptions = bridgeOptions;
		_psim = psim;
		_sanitiser = new HtmlSanitizer();
		discord.Client.MessageReceived += ClientOnMessageReceived;
	}

	private async Task ClientOnMessageReceived(SocketMessage msg)
	{
		if (msg.Author.Id == _discord.Client.CurrentUser.Id)
			return;

		if (msg.Channel is not ITextChannel channel)
			return;

		var guildId = channel.GuildId;
		var config = _bridgeOptions.LinkedGuilds.FirstOrDefault(linkedGuild => linkedGuild.GuildId == guildId);

		// if the current discord server doesn't support bridge, move on
		if (config == null)
			return;

		var channelId = channel.Id;

		// if this isn't a message in the bridge channel, move on
		if (config.BridgeRoom != channelId)
			return;

		var userId = msg.Author.Id;
		var user = await _database.AccountLinks.Query.FirstOrDefaultAsync(accountLink => accountLink.DiscordId == userId);

		// if the user has not linked their psim account, move on
		if (user == null)
		{
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var userDetails = await _psim.Client.GetUserDetails(user.PsimId, TimeSpan.FromSeconds(2));
		var roomRank = userDetails.Rooms.TryGetValue(config.PsimRoom, out var rank) ? rank : Rank.Normal;

		// if the user is globally locked or actively muted in the psim room, move on
		if (userDetails == null || userDetails.GlobalRank == Rank.Locked || 
		    (roomRank is Rank.Locked or Rank.Muted))
		{
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		// if the user is muted/banned but simply not online, move on


		var globalRank = userDetails.GlobalRank;
		var displayRank = (Rank)Math.Max((int)globalRank, (int)roomRank);

		var psimRank = PsimUsername.FromRank(displayRank).Trim();
		var psimName = $"{user.PsimId}";
		var message = msg.CleanContent.Replace("\n", ". ").Trim();

		if (message.ToLowerInvariant().Contains("discord.gg"))
		{
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		try
		{
			message = _sanitiser.Sanitize(message);
		}
		catch
		{
			// ignored
		}

		message = Regex.Replace(message, "\\*\\*(.*?)\\*\\*", "<strong>$1</strong>");
		message = Regex.Replace(message, "__(.*?)__", "<u>$1</u>");
		message = Regex.Replace(message, "\\*(.*?)\\*", "<em>$1</em>");
		message = Regex.Replace(message, "~~(.*?)~~", "<s>$1</s>");

		if (string.IsNullOrEmpty(message))
			return;

		var psimId = $"discord-{msg.Id}";
		var output =
			$"/adduhtml {psimId},<strong><span class=\"username\"><small>{psimRank}</small><username>{psimName}</username></span> <small>[<a href=\"{config.DiscordInviteUrl}\">via Bridge</a>]</small>:</strong> <em>{message}</em>";
		await _psim.Client.Rooms[config.PsimRoom].Send(output);
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

		var name = $"{PsimUsername.FromRank(msg.User.Rank)}{msg.User.DisplayName}".Trim();
		var output = $"**{name}:** {message}";

		if (string.IsNullOrEmpty(output))
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is not ITextChannel channel) 
			return;

		await channel.SendMessageAsync(output);
	}
}
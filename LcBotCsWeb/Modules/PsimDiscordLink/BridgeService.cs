using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Ganss.Xss;
using LcBotCsWeb.Data.Repositories;
using MongoDB.Driver.Linq;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

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

		if (config == null)
			return;

		var channelId = channel.Id;

		if (config.BridgeRoom != channelId)
			return;

		var userId = msg.Author.Id;
		var user = await _database.AccountLinks.Query.FirstOrDefaultAsync(accountLink => accountLink.DiscordId == userId);

		if (user == null)
		{
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var psimName = user.PsimDisplayName;
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
			$"/adduhtml {psimId},<strong><span class=\"username\"><username>{psimName}</username></span> <small>[<a href=\"{config.DiscordInviteUrl}\">via Bridge</a>]</small>:</strong> <em>{message}</em>";
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

		var name = $"({msg.User.Rank}) {msg.User.DisplayName}".Trim();
		var output = $"**{name}:** {message}";

		if (string.IsNullOrEmpty(output))
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is not ITextChannel channel) 
			return;

		await channel.SendMessageAsync(output);
	}
}
using Discord;
using Discord.WebSocket;
using Ganss.Xss;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.PsimDiscordLink;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Enums;
using System;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace LcBotCsWeb.Modules.Bridge;

public class DiscordToPsimBridge
{
	private readonly Database _database;
	private readonly DiscordBotService _discord;
	private readonly BridgeOptions _bridgeOptions;
	private readonly PsimBotService _psim;
	private readonly AltTrackingService _altTracking;
	private readonly PunishmentTrackingService _punishmentTracking;
	private readonly HtmlSanitizer _sanitiser;

	public DiscordToPsimBridge(Database database, DiscordBotService discord, BridgeOptions bridgeOptions, PsimBotService psim,
		AltTrackingService altTracking, PunishmentTrackingService punishmentTracking)
	{
		_database = database;
		_discord = discord;
		_bridgeOptions = bridgeOptions;
		_psim = psim;
		_altTracking = altTracking;
		_punishmentTracking = punishmentTracking;
		_sanitiser = new HtmlSanitizer();
		discord.Client.MessageReceived += ClientOnMessageReceived;
	}

	private async Task ClientOnMessageReceived(SocketMessage msg)
	{
		if (msg.Author.Id == _discord.Client.CurrentUser.Id)
			return;

		if (msg.Channel is not ITextChannel channel)
			return;
		
		var configs = _bridgeOptions.LinkedGuilds.Where(linkedGuild => linkedGuild.GuildId == channel.GuildId);

		// if the current discord server doesn't support bridge, move on
		if (configs == null)
			return;

		// if this isn't a message in the bridge channel, move on
		foreach (var config in configs)
		{
			await MessageReceived(channel, config, msg);
		}
	}

	private async Task MessageReceived(ITextChannel channel, PsimLinkedGuild config, SocketMessage msg)
	{
		if (config.BridgeRoom != channel.Id)
			return;

		var user = await _database.AccountLinks.Query.FirstOrDefaultAsync(accountLink => accountLink.DiscordId == msg.Author.Id);

		// if the user has not linked their psim account, move on
		if (user == null)
		{
			Console.WriteLine($"Failed to send bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because they do not have a linked account");
			await msg.AddReactionAsync(new Emoji("⁉️"));
			return;
		}

		var psimUser = (await _altTracking.GetUser(user.PsimUser))?.FirstOrDefault();

		if (psimUser == null)
		{
			Console.WriteLine($"Failed to send bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) with account link {user.PsimUser} because they do not have an associated PS account (hanging reference)");
			await msg.AddReactionAsync(new Emoji("⁉️"));
			return;
		}

		var userDetails = await _psim.Client.GetUserDetails(psimUser.PsimId, TimeSpan.FromSeconds(2));
		var roomRank = userDetails switch
		{
			null => Rank.Normal,
			_ => userDetails.Rooms.TryGetValue(config.PsimRoom, out var rank) ? rank : Rank.Normal
		};

		if (userDetails?.GlobalRank == Rank.Locked || roomRank is Rank.Locked or Rank.Muted)
		{
			Console.WriteLine(
				$"Failed to send bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because they are actively locked/muted");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		if (await _punishmentTracking.IsUserPunished(user.PsimUser))
		{
			Console.WriteLine(
				$"Failed to send bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because they are actively muted/banned");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var displayRank = (Rank)Math.Max((int)(userDetails?.GlobalRank ?? Rank.Normal), (int)roomRank);
		var psimRank = PsimUsername.FromRank(displayRank).Trim();

		if (msg.Content.ToLowerInvariant().Contains("discord.gg"))
		{
			Console.WriteLine(
				$"Failed to send bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because it contained a link to a discord server");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var lines = msg.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (lines.Length > 16)
		{
			Console.WriteLine(
				$"Failed to send bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because the message was too long");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var reply = msg.Reference?.MessageId.Value;
		if (reply != null && reply.HasValue)
		{
			var replyTo = await channel.GetMessageAsync(reply.Value);
			var author = replyTo.Author;
			var isSelf = author.Id == _discord.Client.CurrentUser.Id;

			if (isSelf)
			{
				// this must be from psim
				var content = replyTo.CleanContent;
				var lineSplit = content.Split("\n");
				var split = lineSplit[0].Split(" - ");
				var replyUser = (split.Length > 0 ? split[1] : split[0]).Trim();
				await SendPsimHtml(config.PsimRoom, $"reply-{msg.Id}",
					$"<small>↱ reply to <strong><span class=\"username\">{replyUser}</span></strong>: {lineSplit[1]}</small>");
			}
			else
			{
				// this must be a discord user
				var replyDiscordUser = await _database.AccountLinks.Query.FirstOrDefaultAsync(accountLink => accountLink.DiscordId == author.Id);
				var replyUser = (await _altTracking.GetUser(replyDiscordUser.PsimUser))?.FirstOrDefault();

				if (replyUser != null)
				{
					var replyMessage = await CleanMessage(replyTo.Content, channel, config.PsimRoom);

					var replyUserDetails = await _psim.Client.GetUserDetails(psimUser.PsimId, TimeSpan.FromSeconds(2));
					var replyRoomRank = replyUserDetails switch
					{
						null => Rank.Normal,
						_ => replyUserDetails.Rooms.TryGetValue(config.PsimRoom, out var rank) ? rank : Rank.Normal
					};

					var replyDisplayRank = (Rank)Math.Max((int)(replyUserDetails?.GlobalRank ?? Rank.Normal),
						(int)replyRoomRank);
					var replyRank = PsimUsername.FromRank(replyDisplayRank).Trim();
					await SendPsimHtml(config.PsimRoom, $"reply-{msg.Id}",
						$"<small>↱ reply to <strong><span class=\"username\">{replyRank}{replyUser.PsimDisplayName}</span></strong>: {replyMessage}</small>");
				}
			}
		}
		
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			await SendLine(line, msg, config.PsimRoom, channel, psimRank, psimUser.PsimDisplayName, config.DiscordInviteUrl, i);
		}
	}

	private async Task SendLine(string message, SocketMessage msg, string psimRoom, ITextChannel channel, string psimRank, string psimName, string inviteUrl, int index)
	{
		message = await CleanMessage(message, channel, psimRoom);
		if (string.IsNullOrEmpty(message))
			return;

		var psimId = $"discord-{msg.Id}-{index}";
		var text = $"<strong><span class=\"username\"><small>{psimRank}</small><username>{psimName}</username></span> <small>[<a href=\"{inviteUrl}\">via LC Discord</a>]</small>:</strong> <em>{message}</em>";
		await SendPsimHtml(psimRoom, psimId, text);
		Console.WriteLine($"Sent {msg.Id} bridge message for {msg.Author.Username} (ID: {msg.Author.Id})");
	}

	private async Task<string> CleanMessage(string message, ITextChannel channel, string psimRoom)
	{
		if (message.ToLowerInvariant().Contains("discord.gg"))
			return string.Empty;

		try
		{
			message = _sanitiser.Sanitize(message);
		}
		catch
		{
			// ignored
		}

		// remember that everything after here is going to be sanitised

		// replace markdown
		message = Regex.Replace(message, "\\*\\*(.*?)\\*\\*", "<strong>$1</strong>");
		message = Regex.Replace(message, "__(.*?)__", "<u>$1</u>");
		message = Regex.Replace(message, "\\*(.*?)\\*", "<em>$1</em>");
		message = Regex.Replace(message, "~~(.*?)~~", "<s>$1</s>");

		// replace emoji with embedded image
		message = await message.ReplaceAsync(new Regex("&lt;:(\\w+):([0-9]+)&gt;", RegexOptions.Singleline), RegexEmote(channel));

		// replace mentions with psim names
		message = await message.ReplaceAsync(new Regex("&lt;@!*&*([0-9]+)&gt;", RegexOptions.Singleline), RegexGetPsimName(channel));
		var roomUsers = _psim.Client.Rooms[psimRoom].Users.OrderByDescending(u => u.DisplayName.Length);

		// replace psim names with username references
		message = roomUsers.Aggregate(message, (current, roomUser) => Regex.Replace(current, $@"\b{roomUser.DisplayName}\b", $"<span class=\"username\"><username>{roomUser.DisplayName}</username></span>", RegexOptions.IgnoreCase));
		return message;

	}

	private async Task SendPsimHtml(string psimRoom, string id, string text)
	{
		var output = $"/adduhtml {id},{text}";
		await _psim.Client.Rooms[psimRoom].Send(output);
	}

	private Func<Match, Task<string>> RegexGetPsimName(ITextChannel channel)
	{
		return async (match) =>
		{
			try
			{
				var id = ulong.Parse(match.Groups[1].Value);
				var user = (await _altTracking.GetUser(id))?.FirstOrDefault();

				if (user != null)
					return $"<span class=\"username\"><username>{user.PsimDisplayName}</username></span>";

				var discordUser = await channel.GetUserAsync(id);

				if (discordUser != null)
					return discordUser.DisplayName;
			}
			catch
			{
				return string.Empty;
			}

			return string.Empty;
		};
	}

	private Func<Match, Task<string>> RegexEmote(ITextChannel channel)
	{
		return (match) =>
		{
			try
			{
				var name = match.Groups[1].Value;
				var emoteId = match.Groups[2].Value;
				var field = $"<:{name}:{emoteId}>";
				var isEmote = Emote.TryParse(field, out var emote);
				var isInternal = channel.Guild.Emotes.Contains(emote);

				return Task.FromResult(isEmote && isInternal
					? $"<img src=\"{emote.Url}\" width=\"16\" height=\"16\" \\>"
					: string.Empty);
			}
			catch
			{
				return Task.FromResult(string.Empty);
			}
		};
	}
}
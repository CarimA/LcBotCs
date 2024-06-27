using Discord;
using Discord.WebSocket;
using Ganss.Xss;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.PsimDiscordLink;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Enums;
using System.Text.RegularExpressions;

namespace LcBotCsWeb.Modules.Bridge;

public class DiscordToPsimBridge
{
	private readonly Database _database;
	private readonly DiscordBotService _discord;
	private readonly BridgeOptions _bridgeOptions;
	private readonly PsimBotService _psim;
	private readonly AltTrackingService _altTracking;
	private readonly VerificationService _verification;
	private readonly PunishmentTrackingService _punishmentTracking;
	private readonly HtmlSanitizer _sanitiser;

	public DiscordToPsimBridge(Database database, DiscordBotService discord, BridgeOptions bridgeOptions, PsimBotService psim,
		AltTrackingService altTracking, VerificationService verification, PunishmentTrackingService punishmentTracking)
	{
		_database = database;
		_discord = discord;
		_bridgeOptions = bridgeOptions;
		_psim = psim;
		_altTracking = altTracking;
		_verification = verification;
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
			await msg.AddReactionAsync(new Emoji("⁉️"));
			return;
		}

		var psimUser = (await _altTracking.GetUser(user.PsimUser))?.FirstOrDefault();

		if (psimUser == null)
		{
			await msg.AddReactionAsync(new Emoji("⁉️"));
			return;
		}

		var userDetails = await _psim.Client.GetUserDetails(psimUser.PsimId, TimeSpan.FromSeconds(2));
		var roomRank = Rank.Normal;

		if (userDetails != null)
		{
			roomRank = userDetails.Rooms.TryGetValue(config.PsimRoom, out var rank) ? rank : Rank.Normal;

			if (userDetails.GlobalRank == Rank.Locked || roomRank is Rank.Locked or Rank.Muted)
			{
				await msg.AddReactionAsync(new Emoji("❌"));
				return;
			}
		}

		if (await _punishmentTracking.IsUserPunished(user.PsimUser))
		{
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var globalRank = userDetails.GlobalRank;
		var displayRank = (Rank)Math.Max((int)globalRank, (int)roomRank);

		var psimRank = PsimUsername.FromRank(displayRank).Trim();
		var psimName = $"{psimUser.PsimDisplayName}";
		var message = msg.Content.Replace("\n", ". ").Trim();

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

		// remember that everything after here is going to be sanitised

		message = Regex.Replace(message, "\\*\\*(.*?)\\*\\*", "<strong>$1</strong>");
		message = Regex.Replace(message, "__(.*?)__", "<u>$1</u>");
		message = Regex.Replace(message, "\\*(.*?)\\*", "<em>$1</em>");
		message = Regex.Replace(message, "~~(.*?)~~", "<s>$1</s>");

		// replace mentions with psim names
		message = await message.ReplaceAsync(new Regex("&lt;@!*&*([0-9]+)&gt;", RegexOptions.Singleline), RegexGetPsimName(channel));

		var roomUsers = _psim.Client.Rooms[config.PsimRoom].Users.OrderByDescending(u => u.DisplayName.Length);

		foreach (var roomUser in roomUsers)
		{
			message = Regex.Replace(message, $"\b{roomUser.DisplayName}\b",
				$"<span class=\"username\"><username>{roomUser.DisplayName}</username></span>");
		}

		if (string.IsNullOrEmpty(message))
			return;

		var psimId = $"discord-{msg.Id}";
		var output =
			$"/adduhtml {psimId},<strong><span class=\"username\"><small>{psimRank}</small><username>{psimName}</username></span> <small>[<a href=\"{config.DiscordInviteUrl}\">via Bridge</a>]</small>:</strong> <em>{message}</em>";
		await _psim.Client.Rooms[config.PsimRoom].Send(output);
	}

	private Func<Match, Task<string>> RegexGetPsimName(ITextChannel channel)
	{
		return async (match) =>
		{
			try
			{
				var id = ulong.Parse(match.Groups[1].Value);
				var user = await _verification.GetVerifiedUserByDiscordId(id);

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
}
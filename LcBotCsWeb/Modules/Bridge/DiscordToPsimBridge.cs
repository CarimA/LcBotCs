using Discord;
using Discord.WebSocket;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.Bridge;

public class DiscordToPsimBridge
{
	private readonly Database _database;
	private readonly DiscordBotService _discord;
	private readonly Configuration _config;
	private readonly PsimBotService _psim;
	private readonly AltTrackingService _altTracking;
	private readonly PunishmentTrackingService _punishmentTracking;

	public DiscordToPsimBridge(Database database, DiscordBotService discord, Configuration config, PsimBotService psim,
		AltTrackingService altTracking, PunishmentTrackingService punishmentTracking)
	{
		_database = database;
		_discord = discord;
		_config = config;
		_psim = psim;
		_altTracking = altTracking;
		_punishmentTracking = punishmentTracking;
		discord.Client.MessageReceived += ClientOnMessageReceived;
	}

	private async Task ClientOnMessageReceived(SocketMessage msg)
	{
		if (msg.Author.Id == _discord.Client.CurrentUser.Id)
			return;

		if (msg.Author.IsBot || msg.Author.IsWebhook)
			return;

		if (msg.Channel is not ITextChannel channel)
			return;

		var configs = _config.BridgedGuilds.Where(linkedGuild => linkedGuild.GuildId == channel.GuildId);

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

		var psimUser = await _altTracking.GetActiveUser(user.PsimUser);

		if (psimUser == null)
		{
			Console.WriteLine($"Failed to send (discord) bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) with account link {user.PsimUser} because they do not have an associated PS account (hanging reference)");
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
				$"Failed to send (discord) bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because they are actively locked/muted");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		if (await _punishmentTracking.IsUserPunished(user.PsimUser))
		{
			Console.WriteLine(
				$"Failed to send (discord) bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because they are actively muted/banned");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var displayRank = (Rank)Math.Max((int)(userDetails?.GlobalRank ?? Rank.Normal), (int)roomRank);
		var psimRank = PsimUsername.FromRank(displayRank).Trim();

		if (msg.Content.ToLowerInvariant().Contains("discord.gg"))
		{
			Console.WriteLine(
				$"Failed to send (discord) bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because it contained a link to a discord server");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		var lines = msg.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (lines.Length > 16)
		{
			Console.WriteLine(
				$"Failed to send (discord) bridge message for {msg.Author.Username} (ID: {msg.Author.Id}) because the message was too long");
			await msg.AddReactionAsync(new Emoji("❌"));
			return;
		}

		try
		{
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
					var split = lineSplit[0].Split(" - ", StringSplitOptions.TrimEntries);
					var replyUser = (split.Length > 1 ? split[1] : lineSplit[0][2..].Trim());
					await _psim.Client.Rooms[config.PsimRoom].SendHtml($"reply-{msg.Id}",
						$"<small>↱ reply to <strong><span class=\"username\">{replyUser}</span></strong>: {lineSplit[1]}</small>");
				}
				else
				{
					// this must be a discord user
					var replyDiscordUser =
						await _database.AccountLinks.Query.FirstOrDefaultAsync(accountLink =>
							accountLink.DiscordId == author.Id);
					var replyUser = await _altTracking.GetActiveUser(replyDiscordUser.PsimUser);

					if (replyUser != null)
					{
						var replyMessage = await CleanMessage(replyTo.Content, channel, config.PsimRoom);

						var replyUserDetails =
							await _psim.Client.GetUserDetails(replyUser.PsimId, TimeSpan.FromSeconds(2));
						var replyRoomRank = replyUserDetails switch
						{
							null => Rank.Normal,
							_ => replyUserDetails.Rooms.TryGetValue(config.PsimRoom, out var rank) ? rank : Rank.Normal
						};

						var replyDisplayRank = (Rank)Math.Max((int)(replyUserDetails?.GlobalRank ?? Rank.Normal),
							(int)replyRoomRank);
						var replyRank = PsimUsername.FromRank(replyDisplayRank).Trim();
						await _psim.Client.Rooms[config.PsimRoom].SendHtml($"reply-{msg.Id}",
							$"<small>↱ reply to <strong><span class=\"username\">{replyRank}{replyUser.PsimDisplayName}</span></strong>: {replyMessage}</small>");
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to prepend a reply for {msg.GetJumpUrl()}");
			Console.WriteLine(ex.Message);
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
		var text = $"<a href=\"{inviteUrl}\"><img src=\"https://lcbotcs-0b1e10f8f000.herokuapp.com/public/discord.png\" width=\"14\" height=\"14\" \\></a> <strong><span class=\"username\"><small>{psimRank}</small><username>{psimName}</username></span>:</strong> <em>{message}</em>";
		await _psim.Client.Rooms[psimRoom].SendHtml(psimId, text);
		Console.WriteLine($"Sent {msg.Id} (discord) bridge message for {msg.Author.Username} (ID: {msg.Author.Id})");
	}

	private async Task<string> CleanMessage(string message, ITextChannel channel, string psimRoom)
	{
		if (message.ToLowerInvariant().Contains("discord.gg"))
			return string.Empty;

		message = message.Sanitise();
		message = message.ParseBasicMarkdown();
		message = await message.ParseEmoji(channel);
		var roomUsers = _psim.Client.Rooms[psimRoom].Users.OrderByDescending(u => u.DisplayName.Length);
		message = await message.ParseMentions(channel, roomUsers, _altTracking);

		return message;
	}
}

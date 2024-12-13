using Discord;
using Discord.Webhook;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Misc;
using MongoDB.Driver;
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
	private DiscordWebhookClient? _webhook;
	private string? _lastDiscordId;

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
		string? avatarUrl = $"https://robohash.org/{name}.png";

		if (psimUserId != null)
		{
			var accountLink = await _db.AccountLinks.Query.FirstOrDefaultAsync(link => link.PsimUser == psimUserId);
			if (accountLink != null)
			{
				discordId = $"{accountLink.DiscordId}";
			}
		}

		var displayRoom = isMultiRoom ? $"[{msg.Room.Name}] " : string.Empty;
		var discordName = (string.IsNullOrEmpty(discordId) || _lastDiscordId == discordId) ? string.Empty : $"-# <@{discordId}>\n";
		var displayName = $"{name}{(displayRoom == string.Empty ? string.Empty : $" (From {displayRoom})")}";

		var output = $"{discordName}{message}";

		if (string.IsNullOrEmpty(output))
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is not ITextChannel channel)
			return;

		if (_webhook == null)
		{
			var webhookConfig = await _db.BridgeWebhooks.Query.FirstOrDefaultAsync(wh => wh.ChannelId == channel.Id && wh.GuildId == guild.Id);

			if (webhookConfig != null)
			{
				_webhook = await TryGetWebhook(webhookConfig, channel) ??
						   await OverwriteWebhookConfig(webhookConfig, channel, guild);
			}
			else
				_webhook = await OverwriteWebhookConfig(null, channel, guild);
		}

		if (discordId != string.Empty)
		{
			try
			{
				var user = await channel.Guild.GetUserAsync(ulong.Parse(discordId), CacheMode.AllowDownload);
				avatarUrl = user.GetAvatarUrl();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				// ignored
			}
		}

		try
		{
			await _webhook.SendMessageAsync(output, username: displayName, avatarUrl: avatarUrl,
				allowedMentions: AllowedMentions.None);
			_lastDiscordId = discordId;
			Console.WriteLine($"Sent (psim) bridge message for {displayName} (webhook)");
		}
		catch (Exception ex1)
		{
			Console.WriteLine($"Webhook failed (output: {output}) (username: {displayName} (avatarUrl: {avatarUrl}), retrying without avatar");
			try
			{
				// try it without an avatar, let's see if that's the issue...
				await _webhook.SendMessageAsync(output, username: displayName,
					allowedMentions: AllowedMentions.None);
				_lastDiscordId = discordId;
				Console.WriteLine($"Sent (psim) bridge message for {displayName} (webhook noavatar)");
			}
			catch (Exception ex2)
			{
				await channel.SendMessageAsync($"-# {displayName}\n{message}", allowedMentions: AllowedMentions.None);
				Console.WriteLine($"Webhook failed, fallback used: {ex2.Message}");
				Console.WriteLine($"Sent (psim) bridge message for {displayName} (fallback)");
			}
		}
	}

	async Task<DiscordWebhookClient?> TryGetWebhook(BridgeWebhook? webhookConfig, ITextChannel channel)
	{
		if (webhookConfig == null)
			return null;

		try
		{
			var webhookInfo = await channel.GetWebhookAsync(webhookConfig.WebhookId, RequestOptions.Default);
			return webhookInfo == null ? null : new DiscordWebhookClient(webhookInfo);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	async Task<IWebhook> CreateWebhook(ITextChannel channel)
	{
		var webhook = await channel.CreateWebhookAsync("psim-bridge", null, RequestOptions.Default);
		return webhook;
	}

	async Task<DiscordWebhookClient> OverwriteWebhookConfig(BridgeWebhook? webhookConfig, ITextChannel channel, IGuild guild)
	{
		if (webhookConfig != null)
			await _db.BridgeWebhooks.Delete(webhookConfig);

		var newWebhook = await CreateWebhook(channel);
		await _db.BridgeWebhooks.Insert(new BridgeWebhook() { ChannelId = channel.Id, GuildId = guild.Id, WebhookId = newWebhook.Id });
		return new DiscordWebhookClient(newWebhook);
	}
}
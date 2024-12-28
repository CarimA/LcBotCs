using Discord;
using Discord.Webhook;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Misc;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Models;
using PsimCsLib.PubSub;
using System.Text.RegularExpressions;

namespace LcBotCsWeb.Modules.Bridge;

public class PsimToDiscordBridge : ISubscriber<ChatMessage>
{
	private readonly DiscordBotService _discord;
	private readonly Configuration _config;
	private readonly AltTrackingService _altTracking;
	private readonly Database _db;
	private DiscordWebhookClient? _webhook;
	private ulong? _lastDiscordId;

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

		if (string.IsNullOrEmpty(message))
			return;

		if (message.StartsWith('/') || message.StartsWith('!'))
			return;

		var config = _config.BridgedGuilds.FirstOrDefault(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase));

		if (config == null)
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is not ITextChannel channel)
			return;

		var isMultiRoom = _config.BridgedGuilds.Count(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase)) > 1;
		var name = $"{PsimUsername.FromRank(msg.User.Rank)}{msg.User.DisplayName}".Trim();

		var (_, accountLink, activeAlt) = await _altTracking.GetAccountByUsername(msg.User);
		var isFromActiveAlt = accountLink != null &&
		                      string.Equals(activeAlt?.PsimId, msg.User.Token,
			                      StringComparison.InvariantCultureIgnoreCase);

		var avatarUrl = isFromActiveAlt
			? (await channel.Guild.GetUserAsync(accountLink.DiscordId)).GetAvatarUrl()
			: $"https://robohash.org/{RemoveSpecialCharacters(name)}.png";

		var discordTag = accountLink != null && isFromActiveAlt && (_lastDiscordId == null || _lastDiscordId == accountLink.DiscordId) ? $"-# <@{accountLink.DiscordId}>\n" : string.Empty;
		var displayName = $"{name}{(isMultiRoom ? $" (From {msg.Room.Name})" : string.Empty)}";
		var output = $"{discordTag}{message}";

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

		var retry = false;

		try
		{
			await _webhook.SendMessageAsync(output, username: displayName, avatarUrl: avatarUrl,
				allowedMentions: AllowedMentions.None);
			Console.WriteLine($"Sent (psim) bridge message for {displayName} (webhook)");
			_lastDiscordId = accountLink?.DiscordId;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Webhook failed (output: {message}) (username: {displayName} (avatarUrl: {avatarUrl}), retrying without avatar");
			Console.WriteLine(ex.Message);
			retry = true;
		}

		if (!retry)
		{
			return;
		}

		retry = false;

		try
		{
			// try it without an avatar, let's see if that's the issue...
			await _webhook.SendMessageAsync(output, username: displayName,
				allowedMentions: AllowedMentions.None);
			Console.WriteLine($"Sent (psim) bridge message for {displayName} (webhook noavatar)");
			_lastDiscordId = accountLink?.DiscordId;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Webhook failed, fallback used: {ex.Message}");
			Console.WriteLine($"Sent (psim) bridge message for {displayName} (fallback)");
			retry = true;
		}

		if (!retry)
		{
			return;
		}

		await channel.SendMessageAsync($"-# {displayName}\n{output}", allowedMentions: AllowedMentions.None);
		_lastDiscordId = accountLink?.DiscordId;
	}

	private static string RemoveSpecialCharacters(string str) => Regex.Replace(str, "[^a-zA-Z0-9_. ]+", "", RegexOptions.Compiled);
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
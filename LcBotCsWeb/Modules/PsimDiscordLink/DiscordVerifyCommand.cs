﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LcBotCsWeb.Modules.AltTracking;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class DiscordVerifyCommand : InteractionModuleBase<SocketInteractionContext>
{
	private readonly VerificationService _verification;
	private readonly AltTrackingService _altTracking;
	private readonly DiscordBotService _discord;
	private readonly Configuration _config;

	public DiscordVerifyCommand(VerificationService verification, AltTrackingService altTracking,
		DiscordBotService discord, Configuration config)
	{
		_verification = verification;
		_altTracking = altTracking;
		_discord = discord;
		_config = config;
		discord.Client.Ready += ClientOnReady;
		discord.Client.UserJoined += ClientOnUserJoined;
	}

	private async Task ClientOnUserJoined(SocketGuildUser user)
	{
		var guildId = user.Guild.Id;

		var config = _config.BridgedGuilds.FirstOrDefault(linkedGuild => linkedGuild.GuildId == guildId);

		if (config == null)
		{
			return;
		}

		var id = user.Id;
		var psimUser = await _verification.GetVerifiedUserByDiscordId(id);

		if (psimUser != null)
		{
			await user.AddRoleAsync(config.RoleId);
			Console.WriteLine($"{psimUser?.PsimId} has rejoined the server and been given bridge access.");
		}
	}

	private async Task ClientOnReady()
	{
		await Task.WhenAll(_config.BridgedGuilds.Select(linkedGuild =>
			_discord.Interaction.RegisterCommandsToGuildAsync(linkedGuild.GuildId)));
	}

	[SlashCommand("link", "Link your Pokémon Showdown account to Discord")]
	public async Task LinkDiscordAccount(string code)
	{
		if (Context.Interaction.User is not IGuildUser user)
			return;

		var id = user.Id;
		var guildId = user.GuildId;
		var config = _config.BridgedGuilds.FirstOrDefault(linkedGuild => linkedGuild.GuildId == guildId);

		if (config == null)
		{
			await RespondAsync("This command is not supported on this server.", null, false, true);
			return;
		}

		var psimUser = await _verification.GetVerifiedUserByDiscordId(id);
		if (psimUser != null)
		{
			await user.AddRoleAsync(config.RoleId);
			await RespondAsync("You have already linked a Pokémon Showdown account to your Discord account.", null, false, true);
			return;
		}

		code = code.Trim().ToLowerInvariant();
		var result = await _verification.MatchCode(code);

		if (result == null || await _verification.IsVerificationCodeNullOrExpired(result))
		{
			await RespondAsync($"`{code}` is an invalid or expired code.", null, false, true);
			return;
		}

		await _verification.Verify(id, result);
		await user.AddRoleAsync(config.RoleId);
		var alts = await _altTracking.GetActiveUser(result.PsimUser);
		await RespondAsync($"{alts?.PsimDisplayName} on Pokémon Showdown has been linked to your Discord account!", null, false, true);
		Console.WriteLine($"{alts?.PsimId} has connected their Showdown account to {user.DisplayName}");
	}
}
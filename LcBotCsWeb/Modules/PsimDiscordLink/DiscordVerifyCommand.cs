using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Discord;
using Discord.Interactions;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Commands;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class DiscordVerifyCommand : InteractionModuleBase<SocketInteractionContext>
{
	private readonly VerificationService _verification;
	private readonly AltTrackingService _altTracking;
	private readonly DiscordBotService _discord;
	private readonly IServiceProvider _serviceProvider;
	private readonly BridgeOptions _bridgeOptions;

	public DiscordVerifyCommand(VerificationService verification, AltTrackingService altTracking, DiscordBotService discord, 
		IServiceProvider serviceProvider, BridgeOptions bridgeOptions)
	{
		_verification = verification;
		_altTracking = altTracking;
		_discord = discord;
		_serviceProvider = serviceProvider;
		_bridgeOptions = bridgeOptions;
		discord.Client.Ready += ClientOnReady;
	}

	private async Task ClientOnReady()
	{
		await _discord.Interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

		await Task.WhenAll(_bridgeOptions.LinkedGuilds.Select(linkedGuild =>
			_discord.Interaction.RegisterCommandsToGuildAsync(linkedGuild.GuildId)));
	}

	[SlashCommand("link", "Link your Pokémon Showdown account to Discord")]
	public async Task LinkDiscordAccount(string code)
	{
		if (Context.Interaction.User is not IGuildUser user)
			return;

		var id = user.Id;
		var guildId = user.GuildId;
		var config = _bridgeOptions.LinkedGuilds.FirstOrDefault(linkedGuild => linkedGuild.GuildId == guildId);

		if (config == null)
		{
			await RespondAsync("This command is not supported on this server.", null, false, true);
			return;
		}

		if ((await _verification.IsDiscordIdVerified(id)))
		{
			await user.AddRoleAsync(config.RoleId);
			await RespondAsync("You have already linked a Pokémon Showdown account to your Discord account.", null, false, true);
			return;
		}

		code = code.Trim().ToLowerInvariant();
		var result = await _verification.MatchCode(code);

		if (await _verification.IsVerificationCodeNullOrExpired(result))
		{
			await RespondAsync($"`{code}` is an invalid or expired code.", null, false, true);
			return;
		}

		await _verification.Verify(id, result);
		await user.AddRoleAsync(config.RoleId);
		var alts = await _altTracking.GetUser(result.PsimUser);
		await RespondAsync($"{alts?.Active.PsimDisplayName} on Pokémon Showdown have been linked to your Discord account!", null, false, true);
	}
}
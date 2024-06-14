using Discord;
using Discord.Interactions;

namespace LcBotCsWeb.Modules.Misc;

public class DiscordRoleAssignment : InteractionModuleBase<SocketInteractionContext>
{
	private const ulong LcDiscord = 231275118700003328;
	private const ulong MatchesRole = 400043115689541632;
	private readonly DiscordBotService _discord;

	public DiscordRoleAssignment(DiscordBotService discord)
	{
		_discord = discord;
		discord.Client.Ready += ClientOnReady;
	}

	private async Task ClientOnReady()
	{
		await _discord.Interaction.RegisterCommandsToGuildAsync(LcDiscord);
	}

	[SlashCommand("matches", "Toggle the Matches role")]
	public async Task ToggleMatchesRole()
	{
		if (Context.Interaction.User is not IGuildUser { GuildId: LcDiscord } user)
			return;

		if (user.RoleIds.Contains(MatchesRole))
		{
			Console.WriteLine($"Matches role removed from {user.DisplayName}");
			await user.RemoveRoleAsync(MatchesRole);
			await RespondAsync("You have been unassigned the matches role and will no longer be notified of tournament matches.", null, false, true);
		}
		else
		{
			Console.WriteLine($"Matches role assigned to {user.DisplayName}");
			await user.AddRoleAsync(MatchesRole);
			await RespondAsync("You have been assigned the matches role and will be notified of tournament matches.", null, false, true);
		}
	}
}

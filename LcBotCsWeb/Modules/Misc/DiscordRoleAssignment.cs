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

	private async Task ToggleRole(ulong role, string roleAssigned, string roleRemoved)
	{
		if (Context.Interaction.User is not IGuildUser { GuildId: LcDiscord } user)
			return;

		var roleInfo = user.Guild.GetRole(role);

		if (roleInfo == null)
		{
			Console.WriteLine($"Warning: {role} was attempted to used but it did not exist on this guild.");
			return;
		}

		var roleName = roleInfo.Name;

		if (user.RoleIds.Contains(role))
		{
			Console.WriteLine($"{roleName} role removed from {user.DisplayName}");
			await user.RemoveRoleAsync(MatchesRole);
			await RespondAsync(roleRemoved, null, false, true);
		}
		else
		{
			Console.WriteLine($"{roleName} role assigned to {user.DisplayName}");
			await user.AddRoleAsync(MatchesRole);
			await RespondAsync(roleAssigned, null, false, true);
		}
	}

	[SlashCommand("matches", "Toggle the Matches role")]
	public async Task ToggleMatchesRole()
	{
		await ToggleRole(MatchesRole,
			"You have been assigned the matches role and will be notified of tournament matches.",
			"You have been unassigned the matches role and will no longer be notified of tournament matches.");
	}
}

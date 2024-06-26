using Discord;
using Discord.Interactions;
using Discord.WebSocket;

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


	private DateTime? _baldCheckCooldown;

	[SlashCommand("baldcheck", "Check where on the Norwood Scale a user is")]
	public async Task BaldCheck(SocketGuildUser user)
	{
		if (DateTime.UtcNow < _baldCheckCooldown)
			await RespondAsync("On cooldown!", null, false, true);

		_baldCheckCooldown = DateTime.UtcNow + TimeSpan.FromSeconds(5);

		var id = user.Id;
		var name = user.DisplayName;

		if (id is 295323609243844608 or 203660036092854272 or 199994947225649153 or 242299071472074753)
		{
			await RespondAsync("The scale has broken, try as you might there is no oasis to be found in this desert of missing follicles. @ is sadly bald.".Replace("@", name));
			await Task.Delay(TimeSpan.FromSeconds(3));
			await FollowupAsync("Condolences.");

			return;
		}

		var results = new List<string>()
		{
			"Norwood 1, Looking good, @",
			"Norwood 2, Pretty workable, @",
			"Norwood 3, @ is starting to bald",
			"Norwood 3 vertex, Uh oh, @",
			"Norwood 4, Time to consider giving up @",
			"Norwood 5, Baldness is approaching, @",
			"Norwood 6, @...",
			"Norwood 7, you're basically bald at this point @"
		};

		var index = Math.Abs(id.GetHashCode() % results.Count);
		await RespondAsync(results[index].Replace("@", name));
	}
}

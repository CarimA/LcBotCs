using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LcBotCsWeb.Data.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LcBotCsWeb.Modules.Misc;

public class DiscordRoleAssignment : InteractionModuleBase<SocketInteractionContext>
{
	private const ulong LcDiscord = 231275118700003328;
	private const ulong MatchesRole = 400043115689541632;
	private readonly DiscordBotService _discord;
	private readonly Database _database;

	public DiscordRoleAssignment(DiscordBotService discord, Database database)
	{
		_discord = discord;
		_database = database;
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
			Console.WriteLine($"Warning: {role} was attempted to be used but it did not exist on this guild.");
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
		if (_baldCheckCooldown != null && DateTime.UtcNow < _baldCheckCooldown)
		{
			var remainingTime = Math.Max(1, (_baldCheckCooldown - DateTime.UtcNow).Value.Minutes);
			await RespondAsync($"On cooldown for about {remainingTime} minute{(remainingTime == 1 ? string.Empty : "s")}!", null, false, true);
			return;
		}

		_baldCheckCooldown = DateTime.UtcNow + TimeSpan.FromMinutes(10);

		var id = user.Id;
		var name = user.DisplayName;

		if (id is 295323609243844608 or 203660036092854272 or 199994947225649153 or 242299071472074753)
		{
			await RespondAsync("The scale has broken, try as you might there is no oasis to be found in this desert of missing follicles. @ is sadly bald.".Replace("@", name));
			return;
		}

		var results = new List<string>()
		{
			"Norwood 1, looking good, @",
			"Norwood 2, pretty workable, @",
			"Norwood 3, @ is starting to bald",
			"Norwood 3 vertex, uh oh, @",
			"Norwood 4, time to consider giving up @",
			"Norwood 5, baldness is approaching, @",
			"Norwood 6, @...",
			"Norwood 7, you're basically bald at this point @"
		};

		var index = Math.Abs((id + (ulong)DateTime.UtcNow.Day).GetHashCode() % results.Count);
		await RespondAsync(results[index].Replace("@", name));
	}

	[SlashCommand("hotfix", "Administrative command")]
	public async Task HotfixAccountLink(SocketGuildUser user, string psimName)
	{
		if (Context.User.Id != 104711168601415680)
		{
			await RespondAsync("You do not have permission to use this command.", null, false, true);
			return;
		}

		await DeferAsync(true);

		var id = user.Id;
		var accountLinks = await _database.AccountLinks.Query.Where(link => link.DiscordId == id).ToListAsync();

		if (accountLinks == null || accountLinks.Count == 0)
		{
			await FollowupAsync("This user does not have an associated account link", null, false, true);
			return;
		}

		var alts = await _database.Alts.Query.Where(alt => alt.PsimDisplayName == psimName).ToListAsync();

		if (alts == null || alts.Count == 0)
		{
			await FollowupAsync("This PS username has not been registered", null, false, true);
			return;
		}

		var altId = alts.First().AltId;

		foreach (var accountLink in accountLinks)
		{
			accountLink.PsimUser = altId;
			await _database.AccountLinks.Update(accountLink);
		}

		await FollowupAsync("Successfully updated user.", null, false, true);
	}
}

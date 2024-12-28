using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PsimCsLib.Models;

namespace LcBotCsWeb.Modules.Misc;

public class DiscordRoleAssignment : InteractionModuleBase<SocketInteractionContext>
{
	private const ulong LcDiscord = 231275118700003328;
	private const ulong MatchesRole = 400043115689541632;
	private readonly DiscordBotService _discord;
	private readonly Database _database;
	private readonly AltTrackingService _altTracking;
	private readonly Configuration _config;

	public DiscordRoleAssignment(DiscordBotService discord, Database database, AltTrackingService altTracking, Configuration config)
	{
		_discord = discord;
		_database = database;
		_altTracking = altTracking;
		_config = config;
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

	[SlashCommand("checkalts", "Check what Pokémon Showdown accounts you can use when using the bridge channel.")]
	public async Task CheckAlts()
	{
		var (alts, accountLink, activeUser) = await _altTracking.GetAccountByDiscordId(Context.User.Id);

		if (accountLink == null)
		{
			await RespondAsync("You have not linked a Pokémon Showdown account to your Discord account.", ephemeral: true);
			return;
		}

		if (alts == null || alts.Count == 0)
		{
			await RespondAsync("You do not appear to have any alts on Pokémon Showdown.", ephemeral: true);
			return;
		}

		var choices = alts.Select(alt => alt.PsimDisplayName);
		await RespondAsync($"**Currently your name is set to display to **{activeUser.PsimDisplayName}**. You can update your display name to: **{string.Join(", ", choices)}", ephemeral: true);

	}

	[SlashCommand("updatealt", "Update your display name on Pokémon Showdown when using the bridge channel.")]
	public async Task UpdateAlt(string name)
	{
		var (alts, accountLink, activeUser) = await _altTracking.GetAccountByDiscordId(Context.User.Id);

		if (accountLink == null)
		{
			await RespondAsync("You have not linked a Pokémon Showdown account to your Discord account.", ephemeral: true);
			return;
		}

		if (alts == null || alts.Count == 0)
		{
			await RespondAsync("You do not appear to have any alts on Pokémon Showdown.", ephemeral: true);
			return;
		}

		var chosen = alts.FirstOrDefault(alt => String.Equals(alt.PsimDisplayName, name, StringComparison.InvariantCultureIgnoreCase));
		if (chosen == null)
		{
			await RespondAsync(
				"I could not confirm this as one of your alt accounts. Use the command `/checkalts` to check what you can change your name to.", ephemeral: true);
			return;
		}

		await RespondAsync("Updating...", ephemeral: true);
		Console.WriteLine($"Updating {accountLink.DiscordId} display name from {activeUser?.PsimDisplayName} to {chosen.PsimDisplayName}");
		await _altTracking.UpdateActiveUser(accountLink, chosen);
		await FollowupAsync("Display name updated.", ephemeral: true);
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

		var alts = await _database.Alts.Query.Where(alt => alt.PsimId == psimName).ToListAsync();

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


	[SlashCommand("forceunlink", "Administrative command")]
	public async Task ForceUnlinkAccount(SocketGuildUser user, string reason)
	{
		if (Context.User.Id != 104711168601415680)
		{
			await RespondAsync("You do not have permission to use this command.", null, false, true);
			return;
		}

		await DeferAsync(true);

		var guildId = user.Guild.Id;
		var config = _config.BridgedGuilds.FirstOrDefault(linkedGuild => linkedGuild.GuildId == guildId);

		if (config == null)
		{
			await FollowupAsync("This command is not supported on this server.", null, false, true);
			return;
		}


		var id = user.Id;
		var accountLinks = await _database.AccountLinks.Query.Where(link => link.DiscordId == id).ToListAsync();
		var tasks = new List<Task>();

		foreach (var accountLink in accountLinks)
		{
			var alts = await _database.Alts.Query.Where(alt => alt.AltId == accountLink.PsimUser).ToListAsync();

			foreach (var alt in alts)
			{
				tasks.Add(_database.Alts.Delete(alt));
			}
			tasks.Add(_database.AccountLinks.Delete(accountLink));
		}

		await user.RemoveRoleAsync(config.RoleId);
		await Task.WhenAll(tasks);

		await FollowupAsync($"Removed bridge role, account link and alts for <@{id}>.");

		try
		{
			await user.SendMessageAsync(
				$"Hello, your Pokemon Showdown bridge link has been forcibly removed from {user.Guild.Name}. Reason: {reason}.");
		}
		catch
		{
			await FollowupAsync($"Unable to message user to let them know.");
		}
	}
}

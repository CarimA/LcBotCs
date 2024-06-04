using Discord;
using Discord.Interactions;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.Commands;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Enums;
using System.Reflection;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class PsimVerifyCommand : InteractionModuleBase<SocketInteractionContext>, ICommand 
{
	private readonly Database _database;
	private readonly DiscordBotService _discord;
	private readonly BridgeOptions _bridgeOptions;
	private readonly IServiceProvider _serviceProvider;

	public List<string> Aliases => new List<string>() { "link" };
	public string HelpText => string.Empty;
	public Rank RequiredPublicRank => Rank.Administrator;
	public bool AllowPublic => false;
	public Rank RequiredPrivateRank => Rank.Normal;
	public bool AllowPrivate => true;
	public bool AcceptIntro => false;

	public PsimVerifyCommand(Database database, DiscordBotService discord, BridgeOptions bridgeOptions, IServiceProvider serviceProvider)
	{
		_database = database;
		_discord = discord;
		_bridgeOptions = bridgeOptions;
		_serviceProvider = serviceProvider;

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

		if ((await _database.AccountLinks.Find(accountLink => accountLink.DiscordId == id)).Count != 0)
		{
			await user.AddRoleAsync(config.RoleId);
			await RespondAsync("You have already linked a Pokémon Showdown account to your Discord account.", null, false, true);
			return;
		}

		code = code.Trim().ToLowerInvariant();
		var result = await _database.VerificationCodes.Query.FirstOrDefaultAsync(c => c.Code == code);

		if (await IsVerificationCodeNullOrExpired(result))
		{
			await RespondAsync($"`{code}` is an invalid or expired code.", null, false, true);
			return;
		}

		await _database.AccountLinks.Insert(new AccountLinkItem()
		{
			Alts = new HashSet<string> { result.Token },
			DiscordId = id,
			PsimDisplayName = result.DisplayName
		});
		
		await user.AddRoleAsync(config.RoleId);
		await RespondAsync($"{result.DisplayName} on Pokémon Showdown has been linked to your Discord account!", null, false, true);
	}
	
	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, CommandResponse respond)
	{
		var token = user.Token;

		if ((await _database.AccountLinks.Find(accountLink => accountLink.Alts.Contains(token))).Any())
		{
			await respond.Send(CommandTarget.Context, "Your Pokémon Showdown username has already been verified.");
			return;
		}

		var verification = await RetrieveVerificationCode(user.DisplayName, token);

		if (verification == null)
		{
			await respond.Send(CommandTarget.Context, "Something went wrong, try again later. If this issue persists, please let a member of Little Cup staff know.");
			return;
		}

		await respond.Send(CommandTarget.Context, $"Use **/link {verification.Code}** on the Little Cup Discord to connect your account. Your verification code will be valid for 15 minutes.");
	}

	private async Task<VerificationCodeItem?> RetrieveVerificationCode(string displayName, string token)
	{
		var result = await _database.VerificationCodes.Query.FirstOrDefaultAsync(code => code.Token == token);

		if (await IsVerificationCodeNullOrExpired(result))
		{
			result = null;
		}

		if (result == null)
		{
			result = GenerateVerificationCode(displayName, token);
			await _database.VerificationCodes.Insert(result);
		}

		return result;
	}

	private async Task<bool> IsVerificationCodeNullOrExpired(VerificationCodeItem? item)
	{
		if (item == null)
			return true;

		if (DateTime.UtcNow <= item.Expiry) 
			return false;

		await _database.VerificationCodes.Delete(item);
		return true;
	}

	private VerificationCodeItem GenerateVerificationCode(string displayName, string token)
	{
		return new VerificationCodeItem()
		{
			Code = Guid.NewGuid().ToString()[..8].ToLowerInvariant(),
			DisplayName = displayName,
			Token = token,
			Expiry = DateTime.UtcNow + TimeSpan.FromMinutes(15)
		};
	}
}

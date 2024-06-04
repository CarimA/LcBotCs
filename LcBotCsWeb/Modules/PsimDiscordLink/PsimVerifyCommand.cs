using System.Diagnostics;
using System.Diagnostics.Contracts;
using Discord.Interactions;
using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.Commands;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Enums;
using Sprache;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class PsimVerifyCommand : InteractionModuleBase<SocketInteractionContext>, ICommand 
{
	private readonly Database _database;
	private readonly DiscordBotService _discord;

	public List<string> Aliases => new List<string>() { "link" };
	public string HelpText => string.Empty;
	public Rank RequiredPublicRank => Rank.Administrator;
	public bool AllowPublic => false;
	public Rank RequiredPrivateRank => Rank.Normal;
	public bool AllowPrivate => true;
	public bool AcceptIntro => false;

	public PsimVerifyCommand(Database database, DiscordBotService discord)
	{
		_database = database;
		_discord = discord;
	}

	[SlashCommand("link", "Link your Pokémon Showdown account to Discord")]
	public async Task LinkDiscordAccount(string code)
	{
		var id = Context.Interaction.User.Id;

		if ((await _database.AccountLinks.Find(accountLink => accountLink.DiscordId == id)).Any())
		{
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

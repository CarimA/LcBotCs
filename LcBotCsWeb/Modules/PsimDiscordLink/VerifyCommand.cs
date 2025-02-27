using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class VerifyCommand : ICommand
{
	private readonly VerificationService _verification;
	private readonly AltTrackingService _altTracking;
	private readonly PsimBotService _psim;

	public List<string> Aliases => new() { "link" };
	public string HelpText => string.Empty;
	public Rank RequiredPublicRank => Rank.Administrator;
	public bool AllowPublic => false;
	public Rank RequiredPrivateRank => Rank.Normal;
	public bool AllowPrivate => true;
	public bool AcceptIntro => false;

	public VerifyCommand(VerificationService verification, AltTrackingService altTracking, PsimBotService psim)
	{
		_verification = verification;
		_altTracking = altTracking;
		_psim = psim;
	}

	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, CommandResponse respond)
	{
		var token = user.Token;

		var (_, accountLink, _) = await _altTracking.GetAccountByUsername(user);

		if (accountLink != null)
		{
			await respond.Send(CommandTarget.Context, "Your Pokémon Showdown username has already been verified.");
			return;
		}

		var userDetails = await _psim.Client.GetUserDetails(token, TimeSpan.FromSeconds(5));

		if (userDetails == null)
		{
			await respond.Send(CommandTarget.Context,
				"Something went wrong, try again later. If this issue persists, please let a member of Little Cup staff know.");
			return;
		}

		if (!userDetails.IsAutoconfirmed)
		{
			await respond.Send(CommandTarget.Context, "You are not autoconfirmed. Please try again in a few days.");
			return;
		}

		var verification = await _verification.RetrieveVerificationCode(user);

		if (verification == null)
		{
			await respond.Send(CommandTarget.Context, "Something went wrong, try again later. If this issue persists, please let a member of Little Cup staff know.");
			return;
		}

		await respond.Send(CommandTarget.Context, $"Use **/link** with the code {verification.Code} on the Little Cup Discord to connect your account. Your verification code will be valid for 15 minutes.");
		Console.WriteLine($"{user.Token} has started an account link");
	}
}

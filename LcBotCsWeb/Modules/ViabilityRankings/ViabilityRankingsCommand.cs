using LcBotCsWeb.Modules.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.ViabilityRankings;

public class ViabilityRankingsCommand : ICommand
{
	private readonly ViabilityRankingsService _viabilityRankingsService;
	public List<string> Aliases => new List<string>() { "vr" };
	public string HelpText => string.Empty;
	public Rank RequiredPublicRank => Rank.Voice;
	public bool AllowPublic => true;
	public Rank RequiredPrivateRank => Rank.Normal;
	public bool AllowPrivate => true;
	public bool AcceptIntro => false;


	public ViabilityRankingsCommand(ViabilityRankingsService viabilityRankingsService)
	{
		_viabilityRankingsService = viabilityRankingsService;
	}

	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, CommandResponse respond)
	{
		var format = arguments.First()?.ToLowerInvariant().Trim();
		if (format == null)
		{
			await respond.Send(CommandTarget.Context, "Please specify a format.");
			return;
		}

		await respond.SendHtml(CommandTarget.Context, "viability-rankings", "Loading...");

		var results = new List<Rankings>();

		try
		{
			var rankings = await _viabilityRankingsService.GetFormat(format);
			if (rankings != null)
				results.AddRange(rankings);
		}
		catch (HttpRequestException)
		{
			await respond.SendHtml(CommandTarget.Context, "viability-rankings", "There was an error handling your request. Try again later.");
		}

		var html = results.GenerateHtml();
		if (string.IsNullOrWhiteSpace(html))
		{
			await respond.SendHtml(CommandTarget.Context, "viability-rankings", $"Viability rankings for {format} could not be found.");
			return;
		}

		await respond.SendHtml(CommandTarget.Context, $"viability-rankings", html);
	}
}
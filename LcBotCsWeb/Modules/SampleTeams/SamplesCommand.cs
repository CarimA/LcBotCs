using LcBotCsWeb.Modules.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.SampleTeams
{
	public class SamplesCommand : ICommand
	{
		public List<string> Aliases => new List<string>() { "samples" };
		public string HelpText => string.Empty;
		public Rank RequiredPublicRank => Rank.Voice;
		public bool AllowPublic => true;
		public Rank RequiredPrivateRank => Rank.Normal;
		public bool AllowPrivate => true;
		public bool AcceptIntro => false;

		private readonly SampleTeamService _sampleTeamService;

		public SamplesCommand(SampleTeamService sampleTeamService)
		{
			_sampleTeamService = sampleTeamService;
		}

		public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, CommandResponse respond)
		{
			if (!arguments.Any())
			{
				await respond.Send(CommandTarget.Context, "Please specify a format.");
				return;
			}

			await respond.SendHtml(CommandTarget.Context, "expanded-samples", "Loading...");

			var results = new List<TeamPreview>();

			try
			{
				foreach (var arg in arguments)
				{
					var teams = await _sampleTeamService.GetFormat(arg.ToLowerInvariant().Trim());
					if (teams != null)
						results.AddRange(teams);
				}
			}
			catch (HttpRequestException)
			{
				await respond.SendHtml(CommandTarget.Context, "expanded-samples", "There was an error handling your request. Try again later.");
			}

			if (!results.Any())
			{
				await respond.SendHtml(CommandTarget.Context, "expanded-samples", "No sample teams could be found.");
				return;
			}

			if (room != null)
				results = results.Shuffle().Take(6).ToList();

			var html = results.GenerateHtml();
			await respond.SendHtml(CommandTarget.Context, "expanded-samples", html);
		}
	}
}
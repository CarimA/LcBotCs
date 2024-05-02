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

		public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, Func<string, Task> send)
		{
			async Task SendHtmlPage(string name, string html)
			{
				await send(room == null 
					? $"/msgroom lc, /sendhtmlpage {user.Token}, {name}, {html}"
					: $"/adduhtml {name}, {html}");
			}

			await SendHtmlPage("expanded-samples", "Loading...");

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
			catch (HttpRequestException _)
			{
				await SendHtmlPage("expanded-samples", "There was an error handling your request. Try again later.");
			}

			if (!results.Any())
			{
				await SendHtmlPage("expanded-samples", "No sample teams could be found.");
				return;
			}

			var html = TeamHtmlFormatter.Generate(results);
			await SendHtmlPage("expanded-samples", html);
		}
	}
}
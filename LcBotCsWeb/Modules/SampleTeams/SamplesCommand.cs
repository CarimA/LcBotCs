using LcBotCsWeb.Services.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.SampleTeams
{
    public class SamplesCommand : ICommand
	{
		public List<string> Aliases => new List<string>() { "samples" };
		public string HelpText => String.Empty;
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

        public async IAsyncEnumerable<string> Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments)
		{
			var results = new List<TeamPreview>();
			var httpError = false;

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
		        httpError = true;
	        }

			if (httpError)
			{
				yield return "There was an error handling your request. Try again later.";
				yield break;
			}

			if (!results.Any())
			{
				yield return "No sample teams could be found.";
				yield break;
			}

			var html = TeamHtmlFormatter.Generate(results);

			if (room == null)
				yield return $"/msgroom lc, /sendhtmlpage {user.Token}, expanded-samples,{html}";
			else
				yield return $"/adduhtml expanded-samples,{html}";
		}
	}
}
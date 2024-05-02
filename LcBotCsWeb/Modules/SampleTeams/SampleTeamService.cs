using LcBotCsWeb.Data.Interfaces;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace LcBotCsWeb.Modules.SampleTeams
{
	public class SampleTeamService
	{
		private readonly ICache _cache;
		private readonly HttpClient _httpClient;
		private readonly Dictionary<string, string> _formatSamples;

		public SampleTeamService(ICache cache)
		{
			_cache = cache;
			_httpClient = new HttpClient();

			_formatSamples =
				new Dictionary<string, string>
				{
					{ "gen2lc", "https://www.smogon.com/forums/threads/gsc-little-cup.3736694/post-9981182" },
					{ "gen3lc", "https://www.smogon.com/forums/threads/adv-lc.3722418/post-9647417" },
					{ "gen5lc", "https://www.smogon.com/forums/threads/bw-lc.3676193/post-8713480" },
					{ "gen6lc", "https://www.smogon.com/forums/threads/oras-lc.3680254/post-8788793" },
					{ "gen7lc", "https://www.smogon.com/forums/threads/sm-lc.3698490/post-9139651" },
					{ "gen8lc", "https://www.smogon.com/forums/threads/ss-lc.3724530/post-9702239" },
					{ "gen9lc", "https://www.smogon.com/forums/threads/sv-lc-sample-teams.3712989/post-9439821" },
					{ "gen9doubleslc", "https://www.smogon.com/forums/threads/sv-doubles-little-cup.3710957/post-9401052" },
					{ "gen9lcuu", "https://www.smogon.com/forums/threads/sv-lc-uu-metagame-resource-and-discussion-thread.3711750/post-9418851" }
				};
		}

		public async Task<List<TeamPreview>?> GetFormat(string format)
		{
			format = format.ToLowerInvariant().Trim();

			if (!_formatSamples.ContainsKey(format))
				return null;

			var results = await _cache.GetOrCreate($"samples-{format}", () => GenerateFormat(format), TimeSpan.FromDays(1));
			return results;
		}

		private async Task<List<TeamPreview>> GenerateFormat(string format)
		{
			var pastes = await ScrapePokepastes(_formatSamples[format]);
			var teams = await GenerateSampleTeams(pastes);
			return teams;
		}

		private async Task<List<string>> ScrapePokepastes(string smogonThread)
		{
			var post = await _httpClient.GetSmogonThread(smogonThread);
			var pastes = Regex
				.Matches(post, @"(https:\/\/pokepast\.es\/)\w+", RegexOptions.IgnoreCase)
				.Select(match => match.Value)
				.Distinct()
				.ToList();

			return pastes;
		}

		private async Task<List<TeamPreview>> GenerateSampleTeams(List<string> pastes)
		{
			var results = await Task.WhenAll(pastes.Select(GenerateSampleTeam));
			return results.ToList();
		}

		private async Task<TeamPreview> GenerateSampleTeam(string paste)
		{
			var response = await _httpClient.GetStringAsync(paste.EndsWith("/json") ? paste : $"{paste}/json");
			var json = JObject.Parse(response);

			var author = json.GetValue("author")?.ToString() ?? string.Empty;
			var title = json.GetValue("title")?.ToString() ?? string.Empty;
			var data = json.GetValue("paste")?.ToString() ?? string.Empty;

			return new TeamPreview(paste,
				!string.IsNullOrWhiteSpace(author) ? author : "an Unknown Author",
				!string.IsNullOrWhiteSpace(title) ? title : "Unnamed Team",
				data);
		}
	}
}

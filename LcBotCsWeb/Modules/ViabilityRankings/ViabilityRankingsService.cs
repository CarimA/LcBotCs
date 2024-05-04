using System.Text.RegularExpressions;
using LcBotCsWeb.Data.Interfaces;

namespace LcBotCsWeb.Modules.ViabilityRankings;

public class ViabilityRankingsService
{
	private readonly ICache _cache;
	private readonly HttpClient _httpClient;
	private readonly Dictionary<string, string> _threads;

	public ViabilityRankingsService(ICache cache)
	{
		_cache = cache;
		_httpClient = new HttpClient();

		_threads =
			new Dictionary<string, string>
			{
				{ "gen9lc", "https://www.smogon.com/forums/threads/sv-lc-viability-rankings.3712664/#post-9434565" },
				{ "gen9lcuu", "https://www.smogon.com/forums/threads/sv-lc-uu-metagame-resource-and-discussion-thread.3711750/#post-9418853" }
			};
	}

	public async Task<List<Rankings>?> GetFormat(string format)
	{
		format = format.ToLowerInvariant().Trim();

		if (!_threads.ContainsKey(format))
			return null;

		var results = await _cache.GetOrCreate($"vrs-{format}", () => GenerateFormat(format), TimeSpan.FromDays(3));
		return results;
	}

	private async Task<List<Rankings>?> GenerateFormat(string format)
	{
		var html = await _httpClient.GetSmogonThread(_threads[format]);

		if (string.IsNullOrWhiteSpace(html))
			return null;

		var results = new List<Rankings>();
		var regex = new Regex("\">(?:<b>)?(.[-+]{0,1})(?: Rank)?(?:</b>)?</span>([\\s\\S]*?)(?:<br />\\s(<s|<b))", RegexOptions.Multiline | RegexOptions.Compiled);
		var matches = regex.Matches(html);
		foreach (Match match in matches)
		{
			var rank = match.Groups[1].Value;
			var pokemonRegex = Regex.Matches(match.Groups[2].Value, "(?:\':)(.+)(?::\')");
			var pokemon = pokemonRegex.Select(p => p.Groups[1].Value.ToLowerInvariant());
			results.Add(new Rankings(rank, pokemon.ToList()));
		}

		return results;
	}
}
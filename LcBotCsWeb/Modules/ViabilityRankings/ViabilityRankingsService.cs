using System.Text.RegularExpressions;
using LcBotCsWeb.Modules.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.ViabilityRankings;

public class ViabilityRankingsService : ICommand
{
	public List<string> Aliases => new List<string>() { "vr" };
	public string HelpText => string.Empty;
	public Rank RequiredPublicRank => Rank.Voice;
	public bool AllowPublic => true;
	public Rank RequiredPrivateRank => Rank.Normal;
	public bool AllowPrivate => true;
	public bool AcceptIntro => false;

	private readonly Dictionary<string, string> _threads;
	private readonly HttpClient _httpClient;

	public ViabilityRankingsService()
	{
		_httpClient = new HttpClient();

		_threads =
			new Dictionary<string, string>
			{
				{ "gen9lc", "https://www.smogon.com/forums/threads/sv-lc-viability-rankings.3712664/#post-9434565" },
				{ "gen9lcuu", "https://www.smogon.com/forums/threads/sv-lc-uu-metagame-resource-and-discussion-thread.3711750/#post-9418853" }
			};
	}

	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, Func<string, Task> send)
	{
		var isPrivate = room == null;
		async Task SendHtmlPage(string name, string html)
		{
			await send(isPrivate
				? $"/msgroom lc, /sendhtmlpage {user.Token}, {name}, {html}"
				: $"/adduhtml {name}, {html}");
		}

		var format = arguments.FirstOrDefault().ToLowerInvariant();

		if (string.IsNullOrWhiteSpace(format))
			return;

		if (!_threads.TryGetValue(format, out var url))
			return;

		var html = await _httpClient.GetSmogonThread(url);

		if (string.IsNullOrWhiteSpace(html))
			return;

		var regex = new Regex("\">(?:<b>)?(.[-+]{0,1})(?: Rank)?(?:</b>)?</span>([\\s\\S]*?)(?:<br />\\s(<s|<b))", RegexOptions.Multiline | RegexOptions.Compiled);
		var matches = regex.Matches(html);
		var divs = new List<string>();
		foreach (Match match in matches)
		{
			var rank = match.Groups[1].Value;
			var pokemonRegex = Regex.Matches(match.Groups[2].Value, "(?:\':)(.+)(?::\')");
			var pokemon = pokemonRegex
				.Select(p => p.Groups[1].Value.ToLowerInvariant()).Select(pokemon => $"<psicon pokemon='{pokemon}'>");
			var icons = string.Join(string.Empty, pokemon);
			divs.Add($"<div style='display: flex; padding: 8px; box-sizing: border-box;'><div><div style='font-size:100%; text-decoration: none; font-weight: bold; text-align: center; margin-top: 4px;'>{rank}</div><div style='font-size: 80%; text-decoration: none; text-align: center;'>RANK</div></div><div style='margin-left: 8px;'>{icons}</div></div>");
		}

		if (!divs.Any())
			return;

		await SendHtmlPage($"viability-{format}", $"{string.Join(string.Empty, divs)}");
	}
}
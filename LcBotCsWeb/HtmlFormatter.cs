using LcBotCsWeb.Modules.ViabilityRankings;

public static class HtmlFormatter
{
	public static string GenerateHtml(this TeamPreview team)
	{
		var icons = string.Join(string.Empty, team.Pokemon
			.Select(pokemon => $"<psicon pokemon='{pokemon}'>"));

		return $"<div style='display:flex;padding:8px;box-sizing:border-box'><div style='margin-right:8px'><a style='text-decoration:none' href='{team.Link}'>{icons}</a></div> <div><a style='font-size:100%;text-decoration:none;font-weight:bold' href='{team.Link}'>{team.Title}</a><br><a style='font-size:80%;text-decoration:none' href='{team.Link}'>{team.Author} • {team.Link}</a></div></div>";
	}

	public static string GenerateHtml(this List<TeamPreview> teams)
	{
		return string.Join(string.Empty, teams.Select(GenerateHtml));
	}

	public static string GenerateHtml(this Rankings ranking)
	{
		var rank = ranking.Rank;
		var icons = string.Join(string.Empty, ranking.Pokemon.Select(pokemon => $"<psicon pokemon='{pokemon}'>"));

		return $"<div style='display:flex;padding:8px;box-sizing:border-box'><div><div style='font-size:100%;text-decoration:none;font-weight:bold;text-align:center;margin-top:4px'>{rank}</div><div style='font-size:80%;text-decoration:none;text-align:center'>RANK</div></div><div style='margin-left:8px'>{icons}</div></div>";
	}

	public static string GenerateHtml(this List<Rankings> rankings)
	{
		return string.Join(string.Empty, rankings.Select(GenerateHtml));
	}
}
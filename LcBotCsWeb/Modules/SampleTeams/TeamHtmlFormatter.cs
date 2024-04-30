public class TeamHtmlFormatter
{
    public static string Generate(TeamPreview team)
    {
        var icons = string.Join(string.Empty, team.Pokemon.OrderBy(x => x)
            .Select(pokemon => $"<psicon pokemon='{pokemon}'>"));

        return $"<div style='display: inline-flex; padding: 10px; box-sizing: border-box; align-items: center;'><div style='flex-shrink: 0;'>{icons}</div> <div><a style='font-size:120%;' href='{team.Link}'>{team.Title}</a><br><a style='font-size: 70%; text-decoration: none;' href='{team.Link}'>{team.Author} • {team.Link}</a></div></div>";
    }

    public static string Generate(List<TeamPreview> teams)
    {
        return string.Join(string.Empty, teams.Select(Generate));
    }
}
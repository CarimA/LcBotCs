public class TeamHtmlFormatter
{
    public static string Generate(TeamPreview team)
    {
        var icons = string.Join(string.Empty, team.Pokemon
            .Select(pokemon => $"<psicon pokemon='{pokemon}'>"));

        return $"<div style='display: flex; padding: 8px; box-sizing: border-box;'><div style='margin-right: 8px;'><a style='text-decoration: none;' href='{team.Link}'>{icons}</a></div> <div><a style='font-size:100%; text-decoration: none; font-weight: bold;' href='{team.Link}'>{team.Title}</a><br><a style='font-size: 80%; text-decoration: none;' href='{team.Link}'>{team.Author} • {team.Link}</a></div></div>";
    }

    public static string Generate(List<TeamPreview> teams)
    {
        return string.Join(string.Empty, teams.Select(Generate));
    }
}
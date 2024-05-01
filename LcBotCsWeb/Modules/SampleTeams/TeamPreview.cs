using System.Text.RegularExpressions;

public class TeamPreview
{
    public string Link { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }
    public List<string> Pokemon { get; set; }

    public TeamPreview(string link, string author, string title, string rawPaste)
    {
        Link = link;
        Author = author;
        Title = title;
        Pokemon = GeneratePokemonList(rawPaste).ToList();
    }

    public TeamPreview(string link, string author, string title, List<string> pokemon)
    {
        Link = link;
        Author = author;
        Title = title;
        Pokemon = pokemon;
    }

    private static IEnumerable<string> GeneratePokemonList(string data)
    {
        var split = data
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line => line.Trim());
        var seekName = true;

        foreach (var line in split)
        {
            if (string.IsNullOrWhiteSpace(line))
                seekName = true;

            if (!seekName)
                continue;

            var nicknameMatch = Regex.Match(line, @"^([^()=@]*)\s+\(([^()=@]{2,})\)");
            var nameMatch = Regex.Match(line, @"^([^()=@]{2,})");

            if (nicknameMatch.Success)
            {
                yield return nicknameMatch.Groups[2].Value.Trim();
                seekName = false;
            }
            else if (nameMatch.Success)
            {
                yield return nameMatch.Groups[1].Value.Trim();
                seekName = false;
            }
        }
    }
}
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
        Pokemon = GeneratePokemonList(rawPaste);
    }

    public TeamPreview(string link, string author, string title, List<string> pokemon)
    {
        Link = link;
        Author = author;
        Title = title;
        Pokemon = pokemon;
    }

    private static List<string> GeneratePokemonList(string data)
    {
        return data
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(FilterNonNames)
            .Select(GetName)
            .ToList();
    }

    private static bool FilterNonNames(string line) => !(line.Contains(':') || line.Contains('/') || line.EndsWith("Nature") || line.StartsWith("- "));
    private static string GetName(string line)
    {
        var chunk = line.Split('@')[0].Replace("(M)", string.Empty).Replace("(F)", string.Empty);
        return (chunk.Contains('(') && chunk.Contains(')')) ? chunk.Split('(', ')')[1] : chunk;
    }
}
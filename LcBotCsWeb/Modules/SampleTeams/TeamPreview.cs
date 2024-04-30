public class TeamPreview
{
    public string Link { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }
    public List<string> Pokemon { get; set; }

    public TeamPreview(string link, string author, string title, List<string> pokemon)
    {
        Link = link;
        Author = author;
        Title = title;
        Pokemon = pokemon;
    }
}
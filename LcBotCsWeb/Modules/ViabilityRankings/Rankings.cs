namespace LcBotCsWeb.Modules.ViabilityRankings;

public class Rankings
{
	public string Rank { get; set; }
	public List<string> Pokemon { get; set; }

	public Rankings(string rank, List<string> pokemon)
	{
		Rank = rank;
		Pokemon = pokemon;
	}
}
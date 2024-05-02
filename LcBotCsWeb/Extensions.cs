namespace LcBotCsWeb;

public static class Extensions
{
	private static readonly Random _rng = new();

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
	{
		return list.OrderBy(_ => _rng.Next());
	}
}

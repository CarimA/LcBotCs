using System.Text.RegularExpressions;

namespace LcBotCsWeb;

public static class Extensions
{
	private static readonly Random _rng = new();

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
	{
		return list.OrderBy(_ => _rng.Next());
	}

	public static async Task<string> GetSmogonThread(this HttpClient client, string url)
	{
		var matchUrl = Regex.Match(url, @"(www.smogon.com/forums/threads/)");
		if (!matchUrl.Success)
			return string.Empty;

		var postId = Regex.Match(url, @"(?<=post-)(.*)").Value;
		var response = await client.GetStringAsync(url);
		var post = Regex.Match(response, @"(?<=js-post-" + postId + @")(.*?)(?=</article>)",
			RegexOptions.Singleline).Value;
		return post;
	}
}
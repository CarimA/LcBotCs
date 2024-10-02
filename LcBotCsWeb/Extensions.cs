using Discord;
using System.Text;
using System.Text.RegularExpressions;
using LcBotCsWeb.Modules.AltTracking;
using Ganss.Xss;
using PsimCsLib.Entities;

namespace LcBotCsWeb;

public static class Extensions
{
	private static readonly Random _rng = new();
	private static readonly HtmlSanitizer _sanitiser = new();

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

	public static async Task<string> ReplaceAsync(this string input, Regex regex, Func<Match, Task<string>> replacementFn)
	{
		var sb = new StringBuilder();
		var lastIndex = 0;

		foreach (Match match in regex.Matches(input))
		{
			sb.Append(input, lastIndex, match.Index - lastIndex)
				.Append(await replacementFn(match).ConfigureAwait(false));

			lastIndex = match.Index + match.Length;
		}

		sb.Append(input, lastIndex, input.Length - lastIndex);
		return sb.ToString();
	}

	public static Func<Match, Task<string>> MatchPsimName(this ITextChannel channel, AltTrackingService altTracking)
	{
		return async (match) =>
		{
			try
			{
				var id = ulong.Parse(match.Groups[1].Value);
				var user = (await altTracking.GetUser(id))?.FirstOrDefault();

				if (user != null)
					return $"<span class=\"username\"><username>{user.PsimDisplayName}</username></span>";

				var discordUser = await channel.GetUserAsync(id);

				if (discordUser != null)
					return discordUser.DisplayName;
			}
			catch
			{
				return string.Empty;
			}

			return string.Empty;
		};
	}

	public static Func<Match, Task<string>> MatchEmoji(this ITextChannel channel)
	{
		return (match) =>
		{
			try
			{
				var name = match.Groups[1].Value;
				var emoteId = match.Groups[2].Value;
				var field = $"<:{name}:{emoteId}>";
				var isEmote = Emote.TryParse(field, out var emote);
				var isInternal = channel.Guild.Emotes.Contains(emote);

				return Task.FromResult(isEmote && isInternal
					? $"<img src=\"{emote.Url}\" width=\"16\" height=\"16\" \\>"
					: string.Empty);
			}
			catch
			{
				return Task.FromResult(string.Empty);
			}
		};
	}

	public static string Sanitise(this string input)
	{
		try
		{
			input = _sanitiser.Sanitize(input);
		}
		catch
		{
			// ignored
		}

		return input;
	}

	public static string ParseBasicMarkdown(this string input)
	{
		input = Regex.Replace(input, "\\*\\*(.*?)\\*\\*", "<strong>$1</strong>");
		input = Regex.Replace(input, "__(.*?)__", "<u>$1</u>");
		input = Regex.Replace(input, "\\*(.*?)\\*", "<em>$1</em>");
		input = Regex.Replace(input, "~~(.*?)~~", "<s>$1</s>");
		return input;
	}

	public static async Task<string> ParseEmoji(this string input, ITextChannel channel)
	{
		input = await input.ReplaceAsync(new Regex("&lt;:(\\w+):([0-9]+)&gt;", RegexOptions.Singleline), channel.MatchEmoji());
		return input;
	}

	public static async Task<string>ParseMentions(this string input, ITextChannel channel, IEnumerable<PsimUsername> users, AltTrackingService altTracking)
	{
		input = await input.ReplaceAsync(new Regex("&lt;@!*&*([0-9]+)&gt;", RegexOptions.Singleline), channel.MatchPsimName(altTracking));
		input = users.Aggregate(input, (current, roomUser) => Regex.Replace(current, $@"\b{roomUser.DisplayName}\b", $"<span class=\"username\"><username>{roomUser.DisplayName}</username></span>", RegexOptions.IgnoreCase));
		return input;
	}
}
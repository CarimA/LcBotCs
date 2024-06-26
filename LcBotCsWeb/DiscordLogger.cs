using Discord;
using System.Diagnostics;
using System.Text;

namespace LcBotCsWeb;

public class DiscordLogger : TextWriter
{
	private readonly DiscordBotService _discord;

	private const ulong LogChannel = 1251118371035021352;

	public DiscordLogger(DiscordBotService discord)
	{
		_discord = discord;
		Console.SetOut(this);
	}

	public override async void WriteLine(string? line)
	{
		Debug.WriteLine(line);

		var channel = await _discord.Client.GetChannelAsync(LogChannel);
		if (channel is not ITextChannel textChannel)
			return;

		await textChannel.SendMessageAsync(line);
		await Task.Delay(500);
	}

	public override Encoding Encoding { get; }
}
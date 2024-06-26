using Discord;
using PsimCsLib.Entities;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Bridge;

public class PsimToDiscordBridge : ISubscriber<ChatMessage>
{
	private readonly DiscordBotService _discord;
	private readonly BridgeOptions _bridgeOptions;

	public PsimToDiscordBridge(DiscordBotService discord, BridgeOptions bridgeOptions)
	{
		_discord = discord;
		_bridgeOptions = bridgeOptions;
	}

	public async Task HandleEvent(ChatMessage msg)
	{
		if (msg.User.DisplayName == Utils.GetEnvVar("PSIM_USERNAME", "PSIM_USERNAME"))
			return;

		if (msg.IsIntro)
			return;

		var message = msg.Message.Trim();

		if (message.StartsWith('/') || message.StartsWith('!'))
			return;

		var config = _bridgeOptions.LinkedGuilds.FirstOrDefault(linkedGuild => string.Equals(linkedGuild.PsimRoom, msg.Room.Name, StringComparison.InvariantCultureIgnoreCase));

		if (config == null)
			return;

		var name = $"{PsimUsername.FromRank(msg.User.Rank)}{msg.User.DisplayName}".Trim();
		var output = $"**{name}:** {message}";
		if (string.IsNullOrEmpty(output))
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is not ITextChannel channel)
			return;

		await channel.SendMessageAsync(output);
	}
}
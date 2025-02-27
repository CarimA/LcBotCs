using LcBotCsWeb.Data.Models;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class BridgeMessage : DatabaseObject
{
	public string HtmlId { get; set; }
	public ulong DiscordId { get; set; }
	public ulong MessageId { get; set; }
	public ulong ChannelId { get; set; }
}
using LcBotCsWeb.Data.Models;

namespace LcBotCsWeb.Modules.Misc;

public class BridgeWebhook : DatabaseObject
{
	public ulong WebhookId { get; set; }
	public ulong GuildId { get; set; }
	public ulong ChannelId { get; set; }
}

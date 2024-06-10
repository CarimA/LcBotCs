using LcBotCsWeb.Data.Models;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class AccountLinkItem : DatabaseObject
{
	public ulong DiscordId { get; set; }
	public string PsimId { get; set; }
	public string PsimDisplayName { get; set; }
}
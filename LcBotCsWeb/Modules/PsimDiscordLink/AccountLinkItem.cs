using LcBotCsWeb.Data.Models;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class AccountLinkItem : DatabaseObject
{
	public HashSet<string> Alts { get; set; }
	public ulong DiscordId { get; set; }
	public string PsimDisplayName { get; set; }
	public DateTime PsimInfractionEnd { get; set; }
}
using LcBotCsWeb.Data.Models;
using MongoDB.Bson;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class AccountLinkItem : DatabaseObject
{
	public ulong DiscordId { get; set; }
	public ObjectId PsimUser { get; set; }
}
using LcBotCsWeb.Data.Models;
using MongoDB.Bson;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class VerificationCodeItem : DatabaseObject
{
	public string Code { get; set; }
	public ObjectId PsimUser { get; set; }
	public DateTime Expiry { get; set; }
}
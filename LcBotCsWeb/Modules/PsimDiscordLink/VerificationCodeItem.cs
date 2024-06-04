using LcBotCsWeb.Data.Models;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class VerificationCodeItem : DatabaseObject
{
	public string Code { get; set; }
	public string Token { get; set; }
	public string DisplayName { get; set; }
	public DateTime Expiry { get; set; }
}
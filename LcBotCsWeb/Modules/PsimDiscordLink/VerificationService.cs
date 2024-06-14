using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class VerificationService
{
	private readonly Database _database;
	private readonly AltTrackingService _altTracking;

	public VerificationService(Database database, AltTrackingService altTracking)
	{
		_database = database;
		_altTracking = altTracking;
	}

	public async Task<VerificationCodeItem?> RetrieveVerificationCode(PsimUsername psimUser)
	{
		var acc = await _altTracking.GetUser(psimUser);

		if (acc == null)
		{
			return null;
		}

		var result = await _database.VerificationCodes.Query.FirstOrDefaultAsync(code => acc.Any(a => a.AltId == code.PsimUser));

		if (await IsVerificationCodeNullOrExpired(result))
		{
			result = null;
		}

		if (result == null)
		{
			result = GenerateVerificationCode(acc.First().AltId);
			await _database.VerificationCodes.Insert(result);
		}

		return result;
	}

	public async Task<bool> IsVerificationCodeNullOrExpired(VerificationCodeItem? item)
	{
		if (item == null)
			return true;

		if (DateTime.UtcNow <= item.Expiry)
			return false;

		await _database.VerificationCodes.Delete(item);
		return true;
	}

	private VerificationCodeItem GenerateVerificationCode(ObjectId id)
	{
		return new VerificationCodeItem()
		{
			Code = Guid.NewGuid().ToString()[..8].ToLowerInvariant(),
			PsimUser = id,
			Expiry = DateTime.UtcNow + TimeSpan.FromMinutes(15)
		};
	}

	private async Task<AccountLinkItem?> GetVerifiedLinkByDiscordId(ulong id)
	{
		return await _database.AccountLinks.Query.FirstOrDefaultAsync(accountLink => accountLink.DiscordId == id);
	}

	public async Task<PsimAlt?> GetVerifiedUserByDiscordId(ulong id)
	{
		var link = await GetVerifiedLinkByDiscordId(id);

		if (link == null)
			return null;

		var user = await _altTracking.GetActiveUser(link.PsimUser);
		return user;
	}

	public async Task<bool> IsUserVerified(PsimUsername username)
	{
		var alts = await _altTracking.GetUser(username);

		if (alts == null)
			return false;

		return await _database.AccountLinks.Query.AnyAsync(accountLink => alts.Any(alt => alt.AltId == accountLink.PsimUser));
	}

	public async Task<VerificationCodeItem?> MatchCode(string code)
	{
		return await _database.VerificationCodes.Query.FirstOrDefaultAsync(c => c.Code == code);
	}

	public async Task Verify(ulong id, VerificationCodeItem result)
	{
		await _database.AccountLinks.Insert(new AccountLinkItem()
		{
			DiscordId = id,
			PsimUser = result.PsimUser
		});

		await _database.VerificationCodes.Delete(result);
	}
}
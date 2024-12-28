using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Modules.AltTracking;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using PsimCsLib.Entities;
using PsimCsLib.Models;

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
		var (alts, accountLink, activeUser) = await _altTracking.GetAccountByUsername(psimUser);

		if (alts == null)
		{
			return null;
		}

		var ids = alts.Select(alt => alt.AltId);
		var result = await _database.VerificationCodes.Query.FirstOrDefaultAsync(code => ids.Contains(code.PsimUser));

		if (await IsVerificationCodeNullOrExpired(result))
		{
			result = null;
		}

		if (result == null)
		{
			result = GenerateVerificationCode(alts.First().AltId);
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
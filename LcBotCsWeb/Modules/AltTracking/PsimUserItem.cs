using LcBotCsWeb.Data.Models;
using MongoDB.Bson;

namespace LcBotCsWeb.Modules.AltTracking;

public class PsimUserItem : DatabaseObject
{
	public List<PsimAlt> Alts { get; set; }
	public PsimAlt Active { get; set; }
	public ObjectId AltId { get; set; }
}
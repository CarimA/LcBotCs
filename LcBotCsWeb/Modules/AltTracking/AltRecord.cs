using LcBotCsWeb.Data.Models;
using MongoDB.Bson;

namespace LcBotCsWeb.Modules.AltTracking;

public class AltRecord : DatabaseObject
{
    public string PsimId { get; set; }
    public ObjectId AltId { get; set; }
}
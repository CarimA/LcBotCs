using MongoDB.Bson;

namespace LcBotCsWeb.Data.Models;

public abstract class DatabaseObject
{
    public ObjectId Id { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}
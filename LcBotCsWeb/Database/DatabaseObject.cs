using MongoDB.Bson;

namespace LcBotCsWeb.Database;

public abstract class DatabaseObject
{
    public ObjectId Id { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }

    protected DatabaseObject()
    {
        DateCreated = DateTime.Now;
        DateModified = DateCreated;
    }
}
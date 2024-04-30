using LcBotCsWeb.Cache;
using LcBotCsWeb.Database.Repository;

namespace LcBotCsWeb.Database;

public interface IDatabase
{
    IRepository<DatabaseCachedObject>? Cache { get; }
}
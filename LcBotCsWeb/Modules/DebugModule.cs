using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Repositories;
using MongoDB.Bson.Serialization;
using PsimCsLib;
using PsimCsLib.Models;
using PsimCsLib.PubSub;
using System.Diagnostics;

namespace LcBotCsWeb.Modules;

public class DebugModule : ISubscriber<LoginSuccess>, ISubscriber<NotImplementedCommand>,
    ISubscriber<SocketConnected>, ISubscriber<SocketDisconnected>
{
    private readonly PsimClient _client;
    private readonly Database _db;
    private readonly ICache _cache;

    public DebugModule(PsimClient client, Database db, ICache cache)
    {
        _client = client;
        _db = db;
        _cache = cache;

        BsonClassMap.RegisterClassMap<WrapperCacheTest>();
    }

    public class WrapperCacheTest
    {
        public string Command { get; set; }
        public List<string> Arguments { get; set; }
    }

    public async Task HandleEvent(NotImplementedCommand e)
    {
        Debug.WriteLine($"{e.Data.Room}, {e.Data.Command}, {string.Join('|', e.Data.Arguments)}");
        var wrapper = new WrapperCacheTest()
        {
            Command = e.Data.Command,
            Arguments = e.Data.Arguments
        };
        var result = await _cache.Get("last-unhandled", () => Task.FromResult(wrapper), TimeSpan.FromMinutes(30));
        Debug.WriteLine(result.Arguments);
    }

    public async Task HandleEvent(LoginSuccess e)
    {
        Debug.WriteLine("Joining botdev...");

        await _client.SetAvatar("supernerd");

        await _client.Rooms.Join("botdevelopment");
        await _client.Rooms.Join("littlecup");
    }

    public async Task HandleEvent(SocketConnected e)
    {
        Debug.WriteLine("Connected client");
    }

    public async Task HandleEvent(SocketDisconnected e)
    {
        Debug.WriteLine($"{e.CloseStatus}: {e.Reason}");
    }
}
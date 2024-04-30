using LcBotCsWeb.Database;
using PsimCsLib;
using PsimCsLib.Models;
using PsimCsLib.PubSub;
using System.Diagnostics;

namespace LcBotCsWeb.Modules;

public class DebugModule : ISubscriber<LoginSuccess>, ISubscriber<NotImplementedCommand>,
    ISubscriber<SocketConnected>, ISubscriber<SocketDisconnected>
{
    private readonly PsimClient _client;
    private readonly IDatabase _db;

    public DebugModule(PsimClient client, IDatabase db)
    {
        _client = client;
        _db = db;
    }

    public async Task HandleEvent(NotImplementedCommand e)
    {
        Debug.WriteLine($"{e.Data.Room}, {e.Data.Command}, {string.Join('|', e.Data.Arguments)}");
    }

    public async Task HandleEvent(LoginSuccess e)
    {
        Debug.WriteLine("Joining botdev...");

        await _client.SetAvatar("supernerd");

        await _client.Rooms.Join("botdevelopment");
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
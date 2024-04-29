using System.Diagnostics;
using PsimCsLib;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LCBotCs;

public class DebugModule : ISubscriber<LoginSuccess>, ISubscriber<NotImplementedCommand>,
    ISubscriber<SocketConnected>, ISubscriber<SocketDisconnected>
{
    private readonly PsimClient _client;

    public DebugModule(PsimClient client)
    {
        _client = client;
    }

    public async Task HandleEvent(NotImplementedCommand e)
    {
        Debug.WriteLine($"{e.Data.Room}, {e.Data.Command}, {string.Join('|', e.Data.Arguments)}");
    }

    public async Task HandleEvent(LoginSuccess e)
    {
        Debug.WriteLine("Joining LC...");

        await _client.SetAvatar("supernerd");
        
        await _client.Rooms.Join("littlecup");
        await _client.Rooms.Join("botdevelopment");
        await _client.Rooms.Join("tournaments");
        await _client.Rooms.Join("help");
        await _client.Rooms.Join("ruinsofalph");
        await _client.Rooms.Join("monotype");
        await _client.Rooms.Join("othermetas");
        await _client.Rooms.Join("techcode");
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
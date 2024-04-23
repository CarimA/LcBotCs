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
        Console.WriteLine($"{e.Data.Room}, {e.Data.Command}, {string.Join('|', e.Data.Arguments)}");
    }

    public async Task HandleEvent(LoginSuccess e)
    {
        Console.WriteLine("Joining LC...");

        await _client.SetAvatar("supernerd");
        await _client.Rooms.Join("littlecup");
        //await client.Rooms.Join("botdevelopment");
    }

    public async Task HandleEvent(SocketConnected e)
    {
        Console.WriteLine("Connected client");
    }

    public async Task HandleEvent(SocketDisconnected e)
    {
        Console.WriteLine($"{e.CloseStatus}: {e.Reason}");
    }
}
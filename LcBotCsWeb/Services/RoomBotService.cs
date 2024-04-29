using PsimCsLib;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Services;

public class RoomBotService : BackgroundService
{
    private readonly PsimClient _psimClient;

    public RoomBotService(IServiceScopeFactory scopeFactory, PsimClient psimClient) : base(scopeFactory)
    {
        _psimClient = psimClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _scopeFactory.CreateScope();
        var modules = scope.ServiceProvider.GetServices<ISubscriber>();

        foreach (var module in modules)
            _psimClient.Subscribe(module);

        await _psimClient.Connect(true);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _psimClient.Disconnect("Cancellation requested");
    }
}
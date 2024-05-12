using PsimCsLib;
using PsimCsLib.PubSub;

namespace LcBotCsWeb;

public class RoomBotService : BackgroundService
{
	private readonly PsimClient _psimClient;
	private readonly IHostApplicationLifetime _lifetime;

	public RoomBotService(IServiceScopeFactory scopeFactory, PsimClient psimClient,
		IHostApplicationLifetime lifetime) : base(scopeFactory)
	{
		_psimClient = psimClient;
		_lifetime = lifetime;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var scope = _scopeFactory.CreateScope();
		var modules = scope.ServiceProvider.GetServices<ISubscriber>();

		foreach (var module in modules)
			_psimClient.Subscribe(module);

		await _psimClient.Connect();
		_lifetime.StopApplication();
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_psimClient.Disconnect("Cancellation requested");
	}
}
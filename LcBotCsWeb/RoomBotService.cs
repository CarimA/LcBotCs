using System.Diagnostics;
using PsimCsLib;
using PsimCsLib.PubSub;

namespace LcBotCsWeb;

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

		while (true)
		{
			try
			{
				await _psimClient.Connect();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				throw;
			}

			await Task.Delay(500);
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_psimClient.Disconnect("Cancellation requested");
	}
}
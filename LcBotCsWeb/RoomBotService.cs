using System.Diagnostics;
using PsimCsLib;
using PsimCsLib.PubSub;

namespace LcBotCsWeb;

public class RoomBotService : BackgroundService
{
	private readonly PsimClient _psimClient;
	private readonly IHostApplicationLifetime _lifeTime;

	public RoomBotService(IServiceScopeFactory scopeFactory, PsimClient psimClient,
		IHostApplicationLifetime lifeTime) : base(scopeFactory)
	{
		_psimClient = psimClient;
		_lifeTime = lifeTime;
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
				await _psimClient.Connect(false);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				throw;
			}

			await Task.Delay(2000);
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_psimClient.Disconnect("Cancellation requested");
		_lifeTime.StopApplication();
	}
}
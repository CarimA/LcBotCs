using System.Diagnostics;
using PsimCsLib;
using PsimCsLib.PubSub;

namespace LcBotCsWeb;

public class PsimBotService : BackgroundService
{
	public PsimClient Client { get; }
	private readonly IHostApplicationLifetime _lifeTime;

	public PsimBotService(IServiceScopeFactory scopeFactory, PsimClientOptions options,
		IHostApplicationLifetime lifeTime) : base(scopeFactory)
	{
		Client = new PsimClient(options);
		_lifeTime = lifeTime;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var scope = _scopeFactory.CreateScope();
		var modules = scope.ServiceProvider.GetServices<ISubscriber>();

		foreach (var module in modules)
			Client.Subscribe(module);

		while (true)
		{
			try
			{
				await Client.Connect(false);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
				throw;
			}

			await Task.Delay(2000);
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		Client.Disconnect("Cancellation requested");
		_lifeTime.StopApplication();
	}
}
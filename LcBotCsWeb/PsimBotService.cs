using PsimCsLib;
using PsimCsLib.PubSub;

namespace LcBotCsWeb;

public class PsimBotService : BackgroundService
{
	public PsimClient Client { get; }
	private readonly IHostApplicationLifetime _lifeTime;

	public PsimBotService(IServiceScopeFactory scopeFactory, Configuration config,
		IHostApplicationLifetime lifeTime) : base(scopeFactory)
	{
		Client = new PsimClient(config.PsimConfiguration);
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
				Console.WriteLine(ex);
				throw;
			}

			await Task.Delay(2000);
		}
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		Client.Disconnect("Cancellation requested");
		_lifeTime.StopApplication();
		return Task.CompletedTask;
	}
}
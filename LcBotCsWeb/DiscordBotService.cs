using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LcBotCsWeb.Modules.PsimDiscordLink;

namespace LcBotCsWeb;

public class DiscordBotService : BackgroundService
{
	public DiscordSocketClient Client { get; }
	private readonly DiscordBotOptions _config;
	private readonly IHostApplicationLifetime _lifeTime;
	private readonly IServiceProvider _serviceProvider;
	private readonly InteractionService _interactionService;

	public DiscordBotService(IServiceScopeFactory scopeFactory, DiscordBotOptions config, IHostApplicationLifetime lifeTime, IServiceProvider serviceProvider) : base(scopeFactory)
	{
		Client = new DiscordSocketClient(new DiscordSocketConfig()
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
		});
		_interactionService = new InteractionService(Client);
		_config = config;
		_lifeTime = lifeTime;
		_serviceProvider = serviceProvider;

		Client.Ready += ClientOnReady;
		Client.Disconnected += async (ex) => await StopAsync(CancellationToken.None);
		Client.MessageReceived += ClientOnMessageReceived;
	}

	private async Task ClientOnMessageReceived(SocketMessage arg)
	{
		System.Diagnostics.Debug.WriteLine($"{arg.Author.GlobalName}: {arg.Content}");
	}

	private async Task ClientOnReady()
	{
		await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

		//await _interactionService.AddModuleAsync<PsimVerifyCommand>(_serviceProvider);

		await _interactionService.RegisterCommandsToGuildAsync(_config.GuildId);

		Client.InteractionCreated += ClientOnInteractionCreated;
	}

	private async Task ClientOnInteractionCreated(SocketInteraction interaction)
	{
		var scope = _serviceProvider.CreateScope();
		var context = new SocketInteractionContext(Client, interaction);
		await _interactionService.ExecuteCommandAsync(context, scope.ServiceProvider);
	}


	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Client.LoginAsync(TokenType.Bot, _config.Token);
		await Client.StartAsync();
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await Client.StopAsync();
		_lifeTime.StopApplication();
	}
}
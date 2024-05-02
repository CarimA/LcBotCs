using PsimCsLib;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Startup;

public class StartupModule : ISubscriber<LoginSuccess>
{
	private readonly PsimClient _client;
	private readonly StartupOptions _options;

	public StartupModule(PsimClient client, StartupOptions options)
	{
		_client = client;
		_options = options;
	}

	public async Task HandleEvent(LoginSuccess e)
	{
		await _client.SetAvatar(_options.Avatar);
		await Task.WhenAll(_options.Rooms.Select(_client.Rooms.Join));
	}
}
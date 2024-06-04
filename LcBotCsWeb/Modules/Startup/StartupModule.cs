using PsimCsLib;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Startup;

public class StartupModule : ISubscriber<LoginSuccess>
{
	private readonly PsimBotService _psim;
	private readonly StartupOptions _options;

	public StartupModule(PsimBotService psim, StartupOptions options)
	{
		_psim = psim;
		_options = options;
	}

	public async Task HandleEvent(LoginSuccess e)
	{
		await _psim.Client.SetAvatar(_options.Avatar);
		await Task.WhenAll(_options.Rooms.Select(_psim.Client.Rooms.Join));
	}
}
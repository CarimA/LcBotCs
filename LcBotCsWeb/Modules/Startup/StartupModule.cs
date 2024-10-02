using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Startup;

public class StartupModule : ISubscriber<LoginSuccess>
{
	private readonly PsimBotService _psim;
	private readonly Configuration _config;

	public StartupModule(PsimBotService psim, Configuration config)
	{
		_psim = psim;
		_config = config;
	}

	public async Task HandleEvent(LoginSuccess e)
	{
		await _psim.Client.SetAvatar(_config.PsimAvatar);
		await Task.WhenAll(_config.PsimRooms.Select(_psim.Client.Rooms.Join));
	}
}
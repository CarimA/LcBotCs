using PsimCsLib.Entities;

namespace LcBotCsWeb.Modules.Commands;

public class CommandResponse
{
	private readonly PsimUsername _user;
	private readonly Room? _room;
	private readonly bool _isPrivate;


	public CommandResponse(PsimUsername user, Room? room, bool isPrivate)
	{
		_user = user;
		_room = room;
		_isPrivate = isPrivate;
	}

	private async Task SendPrivate(string message) => await _user.Send(message);
	private async Task SendPublic(string message) => await _room?.Send(message);

	private async Task SendContext(string privateMsg, string publicMsg)
	{
		if (_isPrivate)
			await SendPrivate(privateMsg);
		else
			await SendPublic(publicMsg);
	}

	private async Task SendContext(string message) => await SendContext(message, message);

	public async Task Send(CommandTarget target, string message)
	{
		switch (target)
		{
			case CommandTarget.Context:
				await SendContext(message);
				break;

			case CommandTarget.Private:
				await SendPrivate(message);
				break;
		}
	}

	public async Task SendHtml(CommandTarget target, string key, string html)
	{
		var privateHtml = $"/msgroom lc, /sendhtmlpage {_user.Token}, {key}, {html}";
		var publicHtml = $"/adduhtml {key}, {html}";

		switch (target)
		{
			case CommandTarget.Context:
				await SendContext(privateHtml, publicHtml);
				break;

			case CommandTarget.Private:
				await SendPrivate(privateHtml);
				break;
		}
	}
}
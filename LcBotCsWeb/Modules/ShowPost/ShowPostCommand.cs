using LcBotCsWeb.Modules.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.ShowPost;

public class ShowPostCommand : ICommand
{
	public List<string> Aliases => new List<string>() { "sp", "showpost" };
	public string HelpText => string.Empty;
	public Rank RequiredPublicRank => Rank.Voice;
	public bool AllowPublic => true;
	public Rank RequiredPrivateRank => Rank.RoomOwner;
	public bool AllowPrivate => false;
	public bool AcceptIntro => false;

	private readonly HttpClient _httpClient;

	public ShowPostCommand()
	{
		_httpClient = new HttpClient();
	}

	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, Func<string, Task> send)
	{
		var url = arguments.FirstOrDefault();
		
		if (string.IsNullOrWhiteSpace(url))
			return;

		var html = await _httpClient.GetSmogonThread(url);

		if (string.IsNullOrWhiteSpace(html))
			return;

		// todo: consider converting the HTML to markdown
		await send($"/adduhtml smogon-thread, {html.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty)}");
	}
}

using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Commands;
using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Modules.PsimDiscordLink;

public class PurgeCommand : ICommand
{
	private readonly PurgeService _purge;
	private readonly PsimBotService _psim;

	public PurgeCommand(PurgeService purge, PsimBotService psim)
	{
		_purge = purge;
		_psim = psim;
	}

	public List<string> Aliases { get; } = ["purge"];
	public string HelpText { get; } = string.Empty;
	public Rank RequiredPublicRank { get; } = Rank.Moderator;
	public bool AllowPublic { get; } = false;
	public Rank RequiredPrivateRank { get; } = Rank.Moderator;
	public bool AllowPrivate { get; } = true;
	public bool AcceptIntro { get; } = false;
	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, CommandResponse respond)
	{
		try
		{
			var target = arguments[0].Trim();
			var username = arguments[1].Trim();
			var reason = arguments.Count > 2 ? arguments[2].Trim() : "none given";
			await _purge.Purge(username, target);
			await _psim.Client.Rooms[target]
				.ModNote($"[{username}]'s messages were purged from Bridge by [{user.Token}] (reason: {reason})");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}
}
public class PurgeBanCommand : ICommand
{
	private readonly PurgeService _purge;
	private readonly PsimBotService _psim;
	private readonly DiscordBotService _discord;

	public PurgeBanCommand(PurgeService purge, PsimBotService psim, DiscordBotService discord)
	{
		_purge = purge;
		_psim = psim;
		_discord = discord;
	}

	public List<string> Aliases { get; } = ["purgeban"];
	public string HelpText { get; } = string.Empty;
	public Rank RequiredPublicRank { get; } = Rank.Moderator;
	public bool AllowPublic { get; } = false;
	public Rank RequiredPrivateRank { get; } = Rank.Moderator;
	public bool AllowPrivate { get; } = true;
	public bool AcceptIntro { get; } = false;
	public async Task Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments, CommandResponse respond)
	{
		try
		{
			var target = arguments[0].Trim();
			var username = arguments[1].Trim();
			var reason = arguments.Count > 2 ? arguments[2].Trim() : "none given";
			await _purge.Purge(username, target, true);
			await _psim.Client.Rooms[target]
				.ModNote($"[{username}]'s messages were purged and banned from Bridge by [{user.Token}] (reason: {reason})");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}
}
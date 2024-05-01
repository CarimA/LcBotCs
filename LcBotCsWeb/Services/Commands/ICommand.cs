using PsimCsLib.Entities;
using PsimCsLib.Enums;

namespace LcBotCsWeb.Services.Commands;

public interface ICommand
{
	public List<string> Aliases { get; }
	public string HelpText { get; }
	public Rank RequiredPublicRank { get; }
	public bool AllowPublic { get; }
	public Rank RequiredPrivateRank { get; }
	public bool AllowPrivate { get; }
	public bool AcceptIntro { get; }

	IAsyncEnumerable<string> Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments);
}
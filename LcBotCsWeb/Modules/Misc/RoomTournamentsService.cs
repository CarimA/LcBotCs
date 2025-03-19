using Discord;
using LcBotCsWeb.Modules.SampleTeams;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.Misc;

public class RoomTournamentsService : ISubscriber<TournamentCreated>
{
	private readonly PsimBotService _psim;
	private readonly DiscordBotService _discord;
	private readonly Configuration _config;
	private readonly SampleTeamService _sampleTeamService;

	public RoomTournamentsService(PsimBotService psim, DiscordBotService discord, Configuration config, SampleTeamService sampleTeamService)
	{
		_psim = psim;
		_discord = discord;
		_config = config;
		_sampleTeamService = sampleTeamService;
	}

	public async Task HandleEvent(TournamentCreated e)
	{
		if (e.IsIntro)
			return;

		var config = _config.BridgedGuilds.FirstOrDefault(linkedGuild => linkedGuild.PsimRoom == e.Room.Name);
		if (config == null)
			return;

		var guild = _discord.Client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
		if (guild?.Channels.FirstOrDefault(c => c.Id == config.BridgeRoom) is ITextChannel channel)
		{
			await channel.SendMessageAsync($"<@&{DiscordRoleAssignment.RoomTourRole}> A {e.Format} is starting on [Pokémon Showdown](https://play.pokemonshowdown.com/{e.Room.Name}).\n-# (Use the `/roomtours` command to be notified when room tournaments start)",
				allowedMentions: AllowedMentions.All);
		}

		var samples = await _sampleTeamService.GetFormat(e.Format);
		var html = samples == null || samples.Count == 0
			? string.Empty
			: samples.Shuffle().Take(6).ToList().GenerateHtml();

		if (html != string.Empty)
			await _psim.Client.Rooms[e.Room.Name].SendHtml("room-tour-samples", html);
	}
}
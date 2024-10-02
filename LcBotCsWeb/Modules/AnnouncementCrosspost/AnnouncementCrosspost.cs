using Discord;
using Discord.WebSocket;
using LcBotCsWeb.Modules.AltTracking;
using PsimCsLib.Entities;

namespace LcBotCsWeb.Modules.AnnouncementCrosspost;

public class AnnouncementCrosspost
{
    private readonly DiscordBotService _discord;
    private readonly Configuration _config;
    private readonly AltTrackingService _altTracking;
    private readonly PsimBotService _psim;

	public AnnouncementCrosspost(DiscordBotService discord, Configuration config, AltTrackingService altTracking, PsimBotService psim)
    {
        _discord = discord;
        _config = config;
        _altTracking = altTracking;
        _psim = psim;
        discord.Client.MessageReceived += ClientOnMessageReceived;
    }

    private async Task ClientOnMessageReceived(SocketMessage msg)
    {
        if (msg.Author.Id == _discord.Client.CurrentUser.Id)
            return;

        if (msg.Channel is not ITextChannel channel)
            return;

        var configs = _config.Crossposts.Where(crosspost => crosspost.GuildId == channel.GuildId && crosspost.RoomId == channel.Id);
        
        foreach (var config in configs)
        {
            await MessageReceived(channel, config, msg);
        }
    }

    private async Task MessageReceived(ITextChannel channel, PsimCrosspost config, SocketMessage msg)
    {
	    var displayName = msg.Author.GlobalName ?? msg.Author.Username;
	    var color = "#ffffff";
	    var avatar = msg.Author.GetAvatarUrl();
	    var message = msg.Content;
	    var id = $"announcement-{DateTime.UtcNow.Ticks}";

	    message = message.Sanitise();
	    message = message.ParseBasicMarkdown();
	    message = await message.ParseEmoji(channel);
	    message = await message.ParseMentions(channel, new List<PsimUsername>(), _altTracking);

	    var text = $"<p><span style=\"font-size: 1rem\">{config.Header}</span><br><img src=\"{avatar}\" width=\"18\" height=\"18\" style=\"vertical-align:bottom;border-radius:50%\"> <strong style=\"color:{color}\">{displayName}</strong></p><p>{message}</p>";
		await _psim.Client.Rooms[config.PsimRoom].SendHtml(id, text);
    }
}
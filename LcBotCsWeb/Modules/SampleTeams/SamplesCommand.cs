using System.Diagnostics;
using PsimCsLib.Enums;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.SampleTeams
{
    public class SamplesCommand : ISubscriber<PrivateMessage>, ISubscriber<ChatMessage>
    {
        private readonly SampleTeamService _sampleTeamService;

        public SamplesCommand(SampleTeamService sampleTeamService)
        {
            _sampleTeamService = sampleTeamService;
        }

        public async Task HandleEvent(PrivateMessage e)
        {
            if (e.Message.StartsWith("-samples"))
            {
                Debug.WriteLine($"message from: {e.Sender.Name.DisplayName}");

                var format = e.Message.Split(' ')[1].ToLowerInvariant().Trim();

                try
                {
                    var results = await _sampleTeamService.GetFormat(format);
                    if (results == null)
                    {
                        await e.Sender.Send($"{format} not found.");
                        return;
                    }

                    var html = TeamHtmlFormatter.Generate(results);
                    await e.Sender.Send($"/msgroom lc, /sendhtmlpage {e.Sender.Name.DisplayName}, expanded-samples,{html}");
                }
                catch (HttpRequestException _)
                {
                    await e.Sender.Send("There was an error handling your request. Try again later.");
                }
            }
        }

        public async Task HandleEvent(ChatMessage e)
        {
            if (e.IsIntro)
                return;

            async Task SendMessage(string message)
            {
                if (e.User.Rank[e.Room] == Rank.Normal)
                    await e.User.Send(message);
                else
                    await e.Room.Send(message);
            }

            async Task SendHtml(string html)
            {
                if (e.User.Rank[e.Room] == Rank.Normal)
                    await e.User.Send($"/msgroom lc, /sendhtmlpage {e.User.Name.DisplayName}, expanded-samples,{html}");
                else
                    await e.Room.Send($"/adduhtml expanded-samples,{html}");
            }

            if (e.Message.StartsWith("~samples"))
            {
                Debug.WriteLine($"message from: {e.User.Name.DisplayName}");

                var format = e.Message.Split(' ')[1].ToLowerInvariant().Trim();

                try
                {
                    var results = await _sampleTeamService.GetFormat(format);
                    if (results == null)
                    {
                        await SendMessage($"{format} not found.");
                        return;
                    }

                    var html = TeamHtmlFormatter.Generate(results);
                    await SendHtml(html);
                }
                catch (HttpRequestException _)
                {
                    await SendMessage("There was an error handling your request. Try again later.");
                }
            }
        }
    }
}
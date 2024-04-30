using System.Diagnostics;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Modules.SampleTeams
{
    public class SamplesCommand : ISubscriber<PrivateMessage>
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
    }
}
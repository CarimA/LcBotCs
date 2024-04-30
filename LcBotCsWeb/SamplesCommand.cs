using LcBotCsWeb.Cache;
using Microsoft.Extensions.Caching.Memory;
using PsimCsLib;
using PsimCsLib.Models;
using PsimCsLib.PubSub;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;

namespace LcBotCsWeb
{
    public class SamplesCommand : ISubscriber<PrivateMessage>
    {
        private readonly ICache _memoryCache;
        private readonly SampleTeamService _sampleTeamService;

        public SamplesCommand(ICache memoryCache, SampleTeamService sampleTeamService)
        {
            _memoryCache = memoryCache;
            _sampleTeamService = sampleTeamService;
        }

        public async Task HandleEvent(PrivateMessage e)
        {
            if (e.Message.StartsWith("-samples"))
            {
                System.Diagnostics.Debug.WriteLine($"message from: {e.Sender.DisplayName}");
                string format = e.Message.Split(' ')[1];

                string html = await _memoryCache.Get<string>(format);

                //try to get samples from cache
                if (html != null)
                {
                    await e.Sender.Send("!code " + html);
                }
                else if (_sampleTeamService.formatSamples.ContainsKey(format))
                {
                    await e.Sender.Send("samples not found, caching samples...");
                    await _sampleTeamService.CacheSamples();
                    if (html != null)
                    {
                        await e.Sender.Send("!code " + html);
                    }
                    else
                    {
                        await e.Sender.Send("there was an error caching samples.");
                    }
                }
                else
                {
                    await e.Sender.Send("there are no samples for that format.");
                }
            }

            if (e.Message.StartsWith("-refreshsamples"))
            {
                System.Diagnostics.Debug.WriteLine($"message from: {e.Sender.DisplayName}");
                await _sampleTeamService.CacheSamples();
                await e.Sender.Send("samples cached (probably)");
            }
        }
    }
}

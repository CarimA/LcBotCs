using System.Diagnostics;
using LcBotCsWeb.Data.Interfaces;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb
{
    public class SamplesCommand : ISubscriber<PrivateMessage>
    {
        private readonly ICache _cache;
        private readonly SampleTeamService _sampleTeamService;

        public SamplesCommand(ICache cache, SampleTeamService sampleTeamService)
        {
            _cache = cache;
            _sampleTeamService = sampleTeamService;
        }

        public async Task HandleEvent(PrivateMessage e)
        {
            if (e.Message.StartsWith("-samples"))
            {
                Debug.WriteLine($"message from: {e.Sender.Name.DisplayName}");

                var format = e.Message.Split(' ')[1];
                var html = await _cache.Get<string>(format);

                //try to get samples from cache
                if (html != null)
                {
                    await e.Sender.Send("!code " + html);
                }
                else if (_sampleTeamService.FormatSamples.ContainsKey(format))
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
                Debug.WriteLine($"message from: {e.Sender.Name.DisplayName}");
                await _sampleTeamService.CacheSamples();
                await e.Sender.Send("samples cached (probably)");
            }
        }
    }
}

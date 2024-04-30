using LcBotCsWeb.Data.Interfaces;
using System.Text.RegularExpressions;
using System.Web;

namespace LcBotCsWeb.Modules.SampleTeams
{
    public class SampleTeamService
    {
        private readonly ICache _cache;
        private readonly HttpClient _httpClient;

        public SampleTeamService(ICache cache)
        {
            _cache = cache;
            _httpClient = new HttpClient();
        }

        //define sample team links. note that the post number is REQUIRED even for original posts of threads
        //you can get the post number from replying to the original post or from inspecting element
        public Dictionary<string, string> FormatSamples =
        new Dictionary<string, string>        {
            { "gen1lc", ""}, //samples planned to be put up sometime after may 5 2024 according to sabelette
            { "gen2lc", "https://www.smogon.com/forums/threads/gsc-little-cup.3736694/post-9981182" },
            { "gen3lc", "https://www.smogon.com/forums/threads/adv-lc.3722418/post-9647417" },
            { "gen4lc", "" }, //no concrete plans for samples. in a worst case i (grape tylenol) play this meta and could try to gather some and ask tazz to put them up, or post them myself
            { "gen5lc", "https://www.smogon.com/forums/threads/bw-lc.3676193/post-8713480" },
            { "gen6lc", "https://www.smogon.com/forums/threads/oras-lc.3680254/post-8788793" },
            { "gen7lc", "https://www.smogon.com/forums/threads/sm-lc.3698490/post-9139651" },
            { "gen8lc", "https://www.smogon.com/forums/threads/ss-lc.3724530/post-9702239" },
            { "gen9lc", "https://www.smogon.com/forums/threads/sv-lc-sample-teams.3712989/post-9439821" },
        };

        public async Task<Team?> GetFormat(string format)
        {
            return new Team();
        }

        public async Task CacheSamples()
        {
            foreach (var (format, thread) in FormatSamples)
            {
                var html = string.Empty;

                if (!string.IsNullOrEmpty(thread))
                {
                    html = await GenerateSamplesHtml(await GrabPokepastesFromSampleThread(format));
                }
                else
                {
                    html = "No samples available for this format :(";
                }

                await _cache.Set(format, html, TimeSpan.FromDays(1));
            }
        }


        public async Task<List<string>> GrabPokepastesFromSampleThread(string format)
        {
            var samplesThread = FormatSamples[format];
            var postId = Regex.Match(samplesThread, @"(?<=post-)(.*)").Value;
            var responseBody = await _httpClient.GetStringAsync(samplesThread);
            var sampleTeamsPost = Regex.Match(responseBody, @"(?<=js-post-" + postId + @")(.*?)(?=</article>)", RegexOptions.Singleline).Value;

            var pokepasteList = new List<string> { };
            foreach (Match match in Regex.Matches(sampleTeamsPost, @"(https:\/\/pokepast\.es\/)\w+", RegexOptions.IgnoreCase))
            {
                pokepasteList.Add(match.Value);
            }
            return pokepasteList;
        }

        public async Task<string> GenerateSamplesHtml(List<string> pokepastes)
        {
            var html = "";
            foreach (var pokepaste in pokepastes)
            {
                try
                {
                    //grab pokepaste source
                    var responseBody = await _httpClient.GetStringAsync(pokepaste);

                    //grab author and title
                    var authorName = Regex.Match(responseBody, @"(?<=<h2>&nbsp;by )(.*?)(?=</h2>)", RegexOptions.Singleline).Value;
                    var teamName = HttpUtility.HtmlDecode(Regex.Match(responseBody, @"(?<=<h1>)(.*?)(?=</h1>)", RegexOptions.Singleline).Value);

                    //grab pokemon names
                    foreach (Match match in Regex.Matches(responseBody, @"(?<=<pre><span class=)(.*?)(?=</span>)", RegexOptions.Singleline))
                    {
                        html += "<psicon pokemon=\"";
                        //can probably make a better regex to avoid making the substring call heere
                        html += match.Value.Substring(match.Value.IndexOf('>') + 1);
                        html += "\"> ";
                    }
                    html += "</br><a href=\"" + pokepaste + "\">";
                    html += teamName;
                    html += "</a> by ";
                    html += authorName;
                    html += "</br>";

                }
                catch (HttpRequestException ex)
                {
                    //todo (lol)
                }
            }
            return html;
        }
    }
}

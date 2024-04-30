using LcBotCsWeb.Data.Interfaces;
using System.Text.RegularExpressions;
using System.Web;

namespace LcBotCsWeb
{
    public class SampleTeamService
    {
        private readonly ICache _memoryCache;
        static readonly HttpClient http_client = new HttpClient();

        public SampleTeamService(ICache memoryCache) =>
            _memoryCache = memoryCache;

        //define sample team links. note that the post number is REQUIRED even for original posts of threads
        //you can get the post number from replying to the original post or from inspecting element
        public Dictionary<string, string> formatSamples =
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

        public async Task CacheSamples()
        {
            foreach (KeyValuePair<string, string> entry in formatSamples)
            {
                string format = entry.Key;
                string html = "";
                if (!string.IsNullOrEmpty(entry.Value))
                {
                    html = await GenerateSamplesHTML(await GrabPokepastesFromSampleThread(format));
                }
                else
                {
                    html = "No samples available for this format :(";
                }

                await _memoryCache.Set(format, html, TimeSpan.FromMilliseconds(86400000));
            }
        }


        public async Task<List<string>> GrabPokepastesFromSampleThread(string format)
        {
            string samples_thread = formatSamples[format];
            string post_id = Regex.Match(samples_thread, @"(?<=post-)(.*)").Value;
            string response_body = await http_client.GetStringAsync(samples_thread);
            string sample_teams_post = Regex.Match(response_body, @"(?<=js-post-" + post_id + @")(.*?)(?=</article>)", RegexOptions.Singleline).Value;

            List<string> pokepaste_list = new List<string> { };
            foreach (Match match in Regex.Matches(sample_teams_post, @"(https:\/\/pokepast\.es\/)\w+", RegexOptions.IgnoreCase))
            {
                pokepaste_list.Add(match.Value);
            }
            return pokepaste_list;
        }
        public async Task<string> GenerateSamplesHTML(List<string> pokepastes)
        {
            string html = "";
            foreach (string pokepaste in pokepastes)
            {
                try
                {
                    //grab pokepaste source
                    string responseBody = await http_client.GetStringAsync(pokepaste);

                    //grab author and title
                    string author_name = Regex.Match(responseBody, @"(?<=<h2>&nbsp;by )(.*?)(?=</h2>)", RegexOptions.Singleline).Value;
                    string team_name = HttpUtility.HtmlDecode(Regex.Match(responseBody, @"(?<=<h1>)(.*?)(?=</h1>)", RegexOptions.Singleline).Value);

                    //grab pokemon names
                    foreach (Match match in Regex.Matches(responseBody, @"(?<=<pre><span class=)(.*?)(?=</span>)", RegexOptions.Singleline))
                    {
                        html += "<psicon pokemon=\"";
                        //can probably make a better regex to avoid making the substring call heere
                        html += match.Value.Substring(match.Value.IndexOf('>') + 1);
                        html += "\"> ";
                    }
                    html += "</br><a href=\"" + pokepaste + "\">";
                    html += team_name;
                    html += "</a> by ";
                    html += author_name;
                    html += "</br>";

                }
                catch (HttpRequestException h_e)
                {
                    //todo (lol)
                }
            }
            return html;
        }
    }
}

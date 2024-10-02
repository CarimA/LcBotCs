using DotNetEnv;
using LcBotCsWeb.Modules.AnnouncementCrosspost;
using PsimCsLib;

namespace LcBotCsWeb;

public class Configuration
{
	public List<PsimLinkedGuild> BridgedGuilds { get; init; }
	public List<PsimCrosspost> Crossposts { get; init; }
	public string DiscordToken { get; init; }
	public string CommandPrefix { get; init; }
	public string PsimAvatar { get; init; }
	public List<string> PsimRooms { get; init; }
	public string DatabaseConnectionString { get; init; }
	public string DatabaseName { get; init; }
	public string? DatabaseCacheCollectionName { get; init; }
	public PsimClientOptions PsimConfiguration { get; init; }

	public static Configuration Load()
	{
		Env.Load();
		var cache = Environment.GetEnvironmentVariable("DATABASE_CACHE_COLLECTION");

		return new Configuration
		{
			BridgedGuilds = LoadConfigFromEnv<List<PsimLinkedGuild>>("BRIDGE_CONFIG"),
			Crossposts = LoadConfigFromEnv<List<PsimCrosspost>>("CROSSPOSTS"),
			DiscordToken = Utils.GetEnvVar("DISCORD_TOKEN", nameof(DiscordToken)),
			CommandPrefix = Utils.GetEnvVar("COMMAND_PREFIX", nameof(CommandPrefix)),
			PsimAvatar = Utils.GetEnvVar("PSIM_AVATAR", nameof(PsimAvatar)),
			PsimRooms = Utils.GetEnvVar("PSIM_ROOMS", nameof(PsimRooms)).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
			DatabaseConnectionString = Utils.GetEnvVar("MONGODB_CONNECTION_STRING", nameof(DatabaseConnectionString)),
			DatabaseName = Utils.GetEnvVar("DATABASE_NAME", nameof(DatabaseName)),
			DatabaseCacheCollectionName = cache,
			PsimConfiguration = new PsimClientOptions()
			{
				Username = Utils.GetEnvVar("PSIM_USERNAME", nameof(PsimConfiguration)),
				Password = Utils.GetEnvVar("PSIM_PASSWORD", nameof(PsimConfiguration))
			}
		};
	}

	private static T LoadConfigFromEnv<T>(string key) where T : class
	{
		return Utils.GetEnvConfig<T>(key, nameof(T));
	}
}
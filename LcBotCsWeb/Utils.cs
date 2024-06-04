using DotNetEnv;
using Newtonsoft.Json;

namespace LcBotCsWeb;

public static class Utils
{
	public static string GetEnvVar(string key, string container)
	{
		return Environment.GetEnvironmentVariable(key) ?? throw new EnvVariableNotFoundException($"{key} not found", container);
	}

	public static T GetEnvConfig<T>(string key, string container)
	{
		return JsonConvert.DeserializeObject<T>(GetEnvVar(key, container) ?? throw new ArgumentNullException($"{key} is malformed"));
	}

}

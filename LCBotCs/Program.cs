using DotNetEnv;
using LCBotCs;
using PsimCsLib;

DotNetEnv.Env.Load();
var options = new PsimClientOptions()
{
     Username = Environment.GetEnvironmentVariable("PSIM_USERNAME") ?? throw new EnvVariableNotFoundException("PSIM_USERNAME not found", nameof(PsimClientOptions)),
     Password = Environment.GetEnvironmentVariable("PSIM_PASSWORD") ?? throw new EnvVariableNotFoundException("PSIM_PASSWORD not found", nameof(PsimClientOptions))
};

var client = new PsimClient(options);
client.Subscribe(new DebugModule(client));

await client.Connect();

using DotNetEnv;
using LcBotCsWeb;
using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Data.Services;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.Bridge;
using LcBotCsWeb.Modules.Commands;
using LcBotCsWeb.Modules.PsimDiscordLink;
using LcBotCsWeb.Modules.SampleTeams;
using LcBotCsWeb.Modules.Startup;
using LcBotCsWeb.Modules.ViabilityRankings;
using Microsoft.Extensions.FileProviders;
using PsimCsLib;
using PsimCsLib.PubSub;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var cache = Environment.GetEnvironmentVariable("DATABASE_CACHE_COLLECTION");

if (string.IsNullOrEmpty(cache))
	builder.Services.AddSingleton<ICache, MemoryCache>();
else
	builder.Services.AddSingleton<ICache, HybridCache>();

void AddHostedService<T>() where T : class, IHostedService
{
	builder.Services.AddHostedService<T>().AddSingleton(x => x.GetServices<IHostedService>().OfType<T>().First());
}

void AddInstance<T>(T instance, bool activate = true) where T : class
{
	var service = builder.Services.AddSingleton(instance);
	if (activate) service.ActivateSingleton<T>();
}

void AddConfig<T>(string key) where T : class
{
	AddInstance(Utils.GetEnvConfig<T>(key, nameof(T)));
}

void AddService<T>(bool activate = true) where T : class
{
	var service = builder.Services.AddSingleton<T>();
	if (activate) service.ActivateSingleton<T>();
}

void AddPsimCommand<T>(bool activate = true) where T : class, ICommand
{
	builder.Services.AddSingleton<T>();
	var service = builder.Services.AddSingleton<ICommand, T>();
	if (activate) service.ActivateSingleton<T>();
}

void AddPsimService<T>(bool activate = true) where T : class, ISubscriber
{
	builder.Services.AddSingleton<T>();
	var service = builder.Services.AddSingleton<ISubscriber, T>();
	if (activate) service.ActivateSingleton<T>();
}

// hosted services are things persistently running in the background - if at any point they stop working, the whole program must shut down
AddHostedService<PsimBotService>();
AddHostedService<DiscordBotService>();

AddConfig<BridgeOptions>("BRIDGE_CONFIG");
AddConfig<DiscordBotOptions>("DISCORD_CONFIG");

// todo: migrate these to the generic config
AddInstance(new CommandOptions { CommandString = Utils.GetEnvVar("COMMAND_PREFIX", nameof(CommandOptions)) });
AddInstance(new StartupOptions(Utils.GetEnvVar("PSIM_AVATAR", nameof(StartupOptions)), Utils.GetEnvVar("PSIM_ROOMS", nameof(StartupOptions))));
AddInstance(new DatabaseOptions
{
	ConnectionString = Utils.GetEnvVar("MONGODB_CONNECTION_STRING", nameof(DatabaseOptions)),
	DatabaseName = Utils.GetEnvVar("DATABASE_NAME", nameof(DatabaseOptions)),
	CacheCollectionName = cache
});
AddInstance(new PsimClientOptions
{
	Username = Utils.GetEnvVar("PSIM_USERNAME", nameof(PsimClientOptions)),
	Password = Utils.GetEnvVar("PSIM_PASSWORD", nameof(PsimClientOptions))
});

AddService<Database>();
AddService<DiscordVerifyCommand>();
AddService<VerificationService>();
AddService<DiscordToPsimBridge>();
AddService<DiscordLogger>();
AddService<ViabilityRankingsService>();
AddService<SampleTeamService>();

AddPsimCommand<ViabilityRankingsCommand>();
AddPsimCommand<SamplesCommand>();
AddPsimCommand<VerifyCommand>();

AddPsimService<CommandService>();
AddPsimService<StartupModule>();
AddPsimService<AltTrackingService>();
AddPsimService<PsimToDiscordBridge>();
AddPsimService<PunishmentTrackingService>();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Static")),
	RequestPath = "/public"
});

app.MapGet("/", () => "Hello World!");

app.Run();

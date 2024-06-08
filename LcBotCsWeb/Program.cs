using DotNetEnv;
using LcBotCsWeb;
using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Data.Services;
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

builder.Services.AddHostedService<PsimBotService>().AddSingleton(x => x.GetServices<IHostedService>().OfType<PsimBotService>().First());
builder.Services.AddSingleton(new PsimClientOptions()
{
	Username = Utils.GetEnvVar("PSIM_USERNAME", nameof(PsimClientOptions)),
	Password = Utils.GetEnvVar("PSIM_PASSWORD", nameof(PsimClientOptions))
});

builder.Services.AddHostedService<DiscordBotService>().AddSingleton(x => x.GetServices<IHostedService>().OfType<DiscordBotService>().First());
builder.Services.AddSingleton(Utils.GetEnvConfig<DiscordBotOptions>("DISCORD_CONFIG", nameof(DiscordBotOptions)));

var cache = Environment.GetEnvironmentVariable("DATABASE_CACHE_COLLECTION");

builder.Services.AddSingleton<Database>();
builder.Services.AddSingleton(new DatabaseOptions()
{
	ConnectionString = Utils.GetEnvVar("MONGODB_CONNECTION_STRING", nameof(DatabaseOptions)),
	DatabaseName = Utils.GetEnvVar("DATABASE_NAME", nameof(DatabaseOptions)),
	CacheCollectionName = cache
});

if (string.IsNullOrEmpty(cache))
	builder.Services.AddSingleton<ICache, MemoryCache>();
else
	builder.Services.AddSingleton<ICache, HybridCache>();

// Register bot modules
builder.Services.AddSingleton<ISubscriber, CommandService>();
builder.Services.AddSingleton(new CommandOptions()
{
	CommandString = Utils.GetEnvVar("COMMAND_PREFIX", nameof(CommandOptions))
});

builder.Services.AddSingleton<ISubscriber, StartupModule>();
builder.Services.AddSingleton(new StartupOptions(Utils.GetEnvVar("PSIM_AVATAR", nameof(StartupOptions)), Utils.GetEnvVar("PSIM_ROOMS", nameof(StartupOptions))));

builder.Services.AddSingleton<SampleTeamService>();
builder.Services.AddSingleton<ICommand, SamplesCommand>();

builder.Services.AddSingleton<ViabilityRankingsService>();
builder.Services.AddSingleton<ICommand, ViabilityRankingsCommand>();

builder.Services.AddSingleton(Utils.GetEnvConfig<BridgeOptions>("BRIDGE_CONFIG", nameof(BridgeOptions)));
builder.Services.AddSingleton<ICommand, PsimVerifyCommand>();
builder.Services.AddSingleton<ISubscriber, BridgeService>();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions()
{
	FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Static")),
	RequestPath = "/public"
});

app.MapGet("/", () => "Hello World!");

app.Run();

using DotNetEnv;
using LcBotCsWeb;
using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Models;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Data.Services;
using LcBotCsWeb.Modules.Commands;
using LcBotCsWeb.Modules.SampleTeams;
using LcBotCsWeb.Modules.Startup;
using LcBotCsWeb.Modules.ViabilityRankings;
using Microsoft.Extensions.FileProviders;
using PsimCsLib;
using PsimCsLib.PubSub;

DotNetEnv.Env.Load();

string GetEnvVar(string key, string container)
{
	return Environment.GetEnvironmentVariable(key) ?? throw new EnvVariableNotFoundException($"{key} not found", container);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHostedService, RoomBotService>();
builder.Services.AddSingleton<PsimClient>();
builder.Services.AddSingleton(new PsimClientOptions()
{
	Username = GetEnvVar("PSIM_USERNAME", nameof(PsimClientOptions)),
	Password = GetEnvVar("PSIM_PASSWORD", nameof(PsimClientOptions))
});

var cache = Environment.GetEnvironmentVariable("DATABASE_CACHE_COLLECTION");

builder.Services.AddSingleton<Database>();
builder.Services.AddSingleton(new DatabaseOptions()
{
	ConnectionString = GetEnvVar("MONGODB_CONNECTION_STRING", nameof(DatabaseOptions)),
	DatabaseName = GetEnvVar("DATABASE_NAME", nameof(DatabaseOptions)),
	CacheCollectionName = cache
});

if (string.IsNullOrEmpty(cache))
	builder.Services.AddSingleton<ICache, MemoryCache>();
else
	builder.Services.AddSingleton<ICache, DatabaseCache>();

// Register bot modules
builder.Services.AddSingleton<ISubscriber, CommandService>();
builder.Services.AddSingleton(new CommandOptions()
{
	CommandString = GetEnvVar("COMMAND_PREFIX", nameof(CommandOptions))
});

builder.Services.AddSingleton<ISubscriber, StartupModule>();
builder.Services.AddSingleton(new StartupOptions(GetEnvVar("PSIM_AVATAR", nameof(StartupOptions)), GetEnvVar("PSIM_ROOMS", nameof(StartupOptions))));

builder.Services.AddSingleton<SampleTeamService>();
builder.Services.AddSingleton<ICommand, SamplesCommand>();

builder.Services.AddSingleton<ViabilityRankingsService>();
builder.Services.AddSingleton<ICommand, ViabilityRankingsCommand>();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions()
{
	FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Static")),
	RequestPath = "/public"
});

app.MapGet("/", () => "Hello World!");

app.Run();

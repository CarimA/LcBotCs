using LcBotCsWeb;
using LcBotCsWeb.Data.Interfaces;
using LcBotCsWeb.Data.Repositories;
using LcBotCsWeb.Data.Services;
using LcBotCsWeb.Modules.AltTracking;
using LcBotCsWeb.Modules.AnnouncementCrosspost;
using LcBotCsWeb.Modules.Bridge;
using LcBotCsWeb.Modules.Commands;
using LcBotCsWeb.Modules.Misc;
using LcBotCsWeb.Modules.PsimDiscordLink;
using LcBotCsWeb.Modules.SampleTeams;
using LcBotCsWeb.Modules.Startup;
using LcBotCsWeb.Modules.ViabilityRankings;
using Microsoft.Extensions.FileProviders;
using PsimCsLib.PubSub;

var builder = WebApplication.CreateBuilder(args);

var config = AddInstance(Configuration.Load());
if (string.IsNullOrEmpty(config.DatabaseCacheCollectionName))
	builder.Services.AddSingleton<ICache, MemoryCache>();
else
	builder.Services.AddSingleton<ICache, HybridCache>();

void AddHostedService<T>() where T : class, IHostedService
{
	builder.Services.AddHostedService<T>().AddSingleton(x => x.GetServices<IHostedService>().OfType<T>().First());
}

T AddInstance<T>(T instance, bool activate = true) where T : class
{
	var service = builder.Services.AddSingleton(instance);
	if (activate) service.ActivateSingleton<T>();
	return instance;
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

AddService<Database>();
AddService<DiscordVerifyCommand>();
AddService<VerificationService>();
AddService<DiscordToPsimBridge>();
AddService<AnnouncementCrosspost>();
AddService<DiscordLogger>();
AddService<ViabilityRankingsService>();
AddService<SampleTeamService>();
AddService<PurgeService>();

AddPsimCommand<ViabilityRankingsCommand>();
AddPsimCommand<SamplesCommand>();
AddPsimCommand<VerifyCommand>();
AddPsimCommand<PurgeCommand>();
AddPsimCommand<PurgeBanCommand>();

AddPsimService<CommandService>();
AddPsimService<RoomTournamentsService>();
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

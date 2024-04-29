using DotNetEnv;
using LcBotCsWeb.Cache;
using LcBotCsWeb.Modules;
using LcBotCsWeb.Services;
using PsimCsLib;
using PsimCsLib.PubSub;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHostedService, RoomBotService>();
builder.Services.AddSingleton<PsimClient>();
builder.Services.AddSingleton(new PsimClientOptions()
{
    Username = Environment.GetEnvironmentVariable("PSIM_USERNAME") ?? throw new EnvVariableNotFoundException("PSIM_USERNAME not found", nameof(PsimClientOptions)),
    Password = Environment.GetEnvironmentVariable("PSIM_PASSWORD") ?? throw new EnvVariableNotFoundException("PSIM_PASSWORD not found", nameof(PsimClientOptions))
});

builder.Services.AddSingleton<ICache, MemoryCache>();

// Register bot modules
builder.Services.AddSingleton<ISubscriber, DebugModule>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
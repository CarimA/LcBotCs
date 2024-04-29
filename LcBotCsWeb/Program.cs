using LCBotCs;
using LcBotCsWeb;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.AddSingleton<IHostedService, RoomBotService>();
builder.Services.AddSingleton<ICache, MemoryCache>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
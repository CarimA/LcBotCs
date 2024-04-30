using DotNetEnv;
using LcBotCsWeb;
using LcBotCsWeb.Cache;
using LcBotCsWeb.Database;
using LcBotCsWeb.Modules;
using LcBotCsWeb.Services;
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

builder.Services.AddSingleton<IDatabase, MongoDbDatabase>();

var cache = Environment.GetEnvironmentVariable("DATABASE_CACHE_COLLECTION");
builder.Services.AddSingleton(new MongoDbDatabaseOptions()
{
    ConnectionString = GetEnvVar("MONGODB_CONNECTION_STRING", nameof(MongoDbDatabaseOptions)),
    DatabaseName = GetEnvVar("DATABASE_NAME", nameof(MongoDbDatabaseOptions)),
    CacheCollectionName = cache
});

if (string.IsNullOrEmpty(cache))
    builder.Services.AddSingleton<ICache, MemoryCache>();
else
    builder.Services.AddSingleton<ICache, DatabaseCache>();

// Register bot modules
builder.Services.AddSingleton<ISubscriber, DebugModule>();

//register samples command
builder.Services.AddSingleton<SampleTeamService>();
builder.Services.AddSingleton<ISubscriber, SamplesCommand>();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "Static")),
    RequestPath = "/public"
});

app.MapGet("/", () => "Hello World!");

app.Run();
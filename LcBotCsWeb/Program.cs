using LcBotCsWeb;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IHostedService, RoomBotService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
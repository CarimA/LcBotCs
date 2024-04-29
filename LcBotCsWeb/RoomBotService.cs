using DotNetEnv;
using LCBotCs;
using PsimCsLib;

namespace LcBotCsWeb;

public class RoomBotService : BackgroundService
{
    public RoomBotService(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope(); 
            
            DotNetEnv.Env.Load();
            var options = new PsimClientOptions()
            {
                Username = Environment.GetEnvironmentVariable("PSIM_USERNAME") ?? throw new EnvVariableNotFoundException("PSIM_USERNAME not found", nameof(PsimClientOptions)),
                Password = Environment.GetEnvironmentVariable("PSIM_PASSWORD") ?? throw new EnvVariableNotFoundException("PSIM_PASSWORD not found", nameof(PsimClientOptions))
            };

            var client = new PsimClient(options);
            client.Subscribe(new DebugModule(client));

            await client.Connect();

            await Task.Delay(500);
        }
    }
}
using LCBotCs;
using PsimCsLib;

var options = new PsimClientOptions()
{
    Username = "",
    Password = ""
};

var client = new PsimClient(options);
client.Subscribe(new DebugModule(client));

await client.Connect();

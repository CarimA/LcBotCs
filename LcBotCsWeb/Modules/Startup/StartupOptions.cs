namespace LcBotCsWeb.Modules.Startup;

public class StartupOptions
{
	public string Avatar { get; set; }
	public List<string> Rooms { get; set; }

	public StartupOptions(string avatar, string rooms)
	{
		Avatar = avatar;
		Rooms = rooms.Split(',').ToList();
	}
}

namespace LcBotCsWeb.Data.Models;

public class DatabaseObjectWrapper<T> : DatabaseObject
{
	public T Data { get; set; }
}

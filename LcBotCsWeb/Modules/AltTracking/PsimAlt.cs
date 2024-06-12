namespace LcBotCsWeb.Modules.AltTracking;

public class PsimAlt : IEquatable<PsimAlt>
{
	public string PsimId { get; set; }
	public string PsimDisplayName { get; set; }

	public bool Equals(PsimAlt? other)
	{
		return PsimId == other?.PsimId;
	}

	public override bool Equals(object? obj)
	{
		return obj?.GetType() == GetType() && Equals((PsimAlt)obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(PsimId, PsimDisplayName);
	}
}
/// <summary>Fired for every buy attempt, so sounds and messages can react to it.</summary>
public class PurchaseSignal
{
	public PurchaseKind Kind;
	public bool Success;
	// Shown to the player when the purchase fails, null when the view explains it itself
	public string FailMessage;
}

public enum PurchaseKind
{
	Planet,
	Upgrade,
	Production,
	Manager
}

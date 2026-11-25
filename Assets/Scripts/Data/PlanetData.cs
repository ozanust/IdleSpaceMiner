[System.Serializable]
public class PlanetData
{
	public int PlanetIndex;
	public bool IsUnraveled;
	public bool IsUnlocked;
	public int MiningRateLevel;
	public int ShipSpeedLevel;
	public int ShipCargoLevel;
	public float CurrentTotalMiningRate;
	public float CurrentShipSpeed;
	public int CurrentShipCargo;
	public long TotalMiningRateUpdatePrice;
	public long ShipSpeedUpdatePrice;
	public long ShipCargoUpdatePrice;
	public ResourceMiningData[] MinedAmounts;

	public PlanetData()
	{

	}
}

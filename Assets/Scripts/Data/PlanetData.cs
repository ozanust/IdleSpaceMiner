[System.Serializable]
public class PlanetData
{
	public int PlanetIndex;
	public bool IsUnraveled;
	public bool IsUnlocked;
	public int MiningRateLevel;
	public int ShipSpeedLevel;
	public int ShipCargoLevel;
	public PlanetMineData[] MinedAmounts;

	public PlanetData()
	{

	}
}

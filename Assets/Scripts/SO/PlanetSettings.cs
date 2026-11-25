using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetSettings", menuName = "Settings/Planets", order = 1)]
public class PlanetSettings : ScriptableObject
{
	[SerializeField] private PlanetDataSetting[] resourceDataSettings;
	public PlanetDataSetting[] Settings => resourceDataSettings;

	public bool TryGetPlanetSetting(int planetId, out PlanetDataSetting setting)
	{
		setting = null;
		for (int i = 0; i < resourceDataSettings.Length; i++)
		{
			if (planetId == resourceDataSettings[i].PlanetId)
			{
				setting = resourceDataSettings[i];
				return true;
			}
		}

		return false;
	}
}

[System.Serializable]
public class PlanetDataSetting
{
	public int PlanetId;
	public long PlanetPrice;
	public PlanetMiningYield[] MiningYieldRatios;
	public float TotalStartingMiningRate;
	public float StartingShipSpeed;
	public int StartingShipCargo;
	public float PriceIncreaseMultiplier;
	public string Name;
	public Sprite Icon;
}

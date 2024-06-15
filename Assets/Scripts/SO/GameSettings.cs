using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game", order = 1)]
public class GameSettings : ScriptableObject
{
	[SerializeField] private EconomySettings economySettings;
	[SerializeField] private GlobalSettings globalSettings;

	public EconomySettings EconomySettings => economySettings;
	public GlobalSettings GlobalSettings => globalSettings;

	public int GetInitialAmount(ResourceType type)
	{
		return 0;
	}
}

[System.Serializable]
public class EconomySettings
{
	public int StartingMoney;
	public int StartingDarkMatter;
}

[System.Serializable]
public class GlobalSettings
{
	public int[] AsteroidSpawnTimes;
}
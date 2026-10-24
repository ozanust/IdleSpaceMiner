using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game", order = 1)]
public class GameSettings : ScriptableObject
{
	[SerializeField] private EconomySettings economySettings;
	[SerializeField] private GlobalSettings globalSettings;
	[SerializeField] private TaskSetting[] tasks;

	// Rewards are Dark Matter
	[SerializeField] private TaskSetting[] achievements = DefaultAchievements();
	[SerializeField] private ManagerSettings managerSettings = new ManagerSettings();

	public EconomySettings EconomySettings => economySettings;
	public GlobalSettings GlobalSettings => globalSettings;
	public TaskSetting[] Tasks => tasks;
	public TaskSetting[] Achievements => achievements;
	public ManagerSettings ManagerSettings => managerSettings;

	private static TaskSetting[] DefaultAchievements()
	{
		return new[]
		{
			Goal(TaskType.UnlockPlanets, 3, 5), Goal(TaskType.UnlockPlanets, 7, 10), Goal(TaskType.UnlockPlanets, 10, 15),
			Goal(TaskType.UnlockPlanets, 13, 20), Goal(TaskType.UnlockPlanets, 16, 30),
			Goal(TaskType.UpgradePlanets, 25, 5), Goal(TaskType.UpgradePlanets, 100, 10), Goal(TaskType.UpgradePlanets, 250, 15),
			Goal(TaskType.UpgradePlanets, 500, 25),
			Goal(TaskType.DestroyAsteroids, 1, 5), Goal(TaskType.DestroyAsteroids, 10, 10), Goal(TaskType.DestroyAsteroids, 50, 20),
			Goal(TaskType.DestroyAsteroids, 100, 30),
			Goal(TaskType.ResearchCount, 3, 5), Goal(TaskType.ResearchCount, 6, 10), Goal(TaskType.ResearchCount, 9, 20),
			Goal(TaskType.ResearchCount, 12, 30),
			Goal(TaskType.HaveSmelters, 2, 10), Goal(TaskType.HaveSmelters, 3, 20), Goal(TaskType.HaveSmelters, 4, 30),
			Goal(TaskType.HaveCrafters, 2, 15), Goal(TaskType.HaveCrafters, 3, 30),
			Goal(TaskType.HireManagers, 1, 5), Goal(TaskType.HireManagers, 3, 10), Goal(TaskType.HireManagers, 5, 15),
			Goal(TaskType.HireManagers, 10, 25),
			Goal(TaskType.HaveMoney, 10000, 5), Goal(TaskType.HaveMoney, 100000, 10), Goal(TaskType.HaveMoney, 1000000, 20),
			Goal(TaskType.HaveMoney, 10000000, 30), Goal(TaskType.HaveMoney, 100000000, 50),
			Goal(TaskType.HaveResource, 100, 10, ResourceType.CopperBar), Goal(TaskType.HaveResource, 10, 25, ResourceType.Circuit),
			Goal(TaskType.HaveResource, 5, 40, ResourceType.Laser),
		};
	}

	private static TaskSetting Goal(TaskType type, int amount, long reward, ResourceType resource = ResourceType.Copper)
	{
		return new TaskSetting { Type = type, TargetAmount = amount, Reward = reward, Resource = resource };
	}

	public int GetInitialAmount(ResourceType type)
	{
		return 0;
	}
}

[System.Serializable]
public class EconomySettings
{
	public long StartingMoney;
	public int StartingDarkMatter;
}

[System.Serializable]
public class GlobalSettings
{
	public int[] AsteroidSpawnTimes;
	// Mining speed while the game is not running, relative to normal speed
	public float OfflineMiningMultiplier;
	public int MaxOfflineSeconds;
	// An asteroid holds this many seconds worth of all planets' mining
	public float AsteroidRewardSeconds;
}

/// <summary>One step of the guided task chain. Only the fields matching Type are used.</summary>
[System.Serializable]
public class TaskSetting
{
	public TaskType Type;
	public int TargetAmount;
	public ResourceType Resource;
	public ResearchType Research;
	public long Reward;
}

/// <summary>Manager rules, defaults from the Idle Planet Miner wiki.</summary>
[System.Serializable]
public class ManagerSettings
{
	public int StartingSlots = 2;
	// The 3rd slot costs ThirdSlotCost, every later slot costs SlotCostPerSlot x (slot number - 1)
	public int ThirdSlotCost = 50;
	public int SlotCostPerSlot = 50;
	public int MaxStars = 7;
	public int ManagersToPromote = 3;

	public ManagerSchoolSetting[] Schools =
	{
		new ManagerSchoolSetting { School = ManagerSchool.Public, Name = "Public School", Cost = 50, Stars = new[] { 1, 2, 3 }, Chances = new[] { 0.75f, 0.2f, 0.05f } },
		new ManagerSchoolSetting { School = ManagerSchool.Private, Name = "Private School", Cost = 300, Stars = new[] { 3, 4 }, Chances = new[] { 0.75f, 0.25f } },
		new ManagerSchoolSetting { School = ManagerSchool.Brilliant, Name = "Brilliant Academy", Cost = 500, Stars = new[] { 4, 5 }, Chances = new[] { 0.75f, 0.25f } },
	};

	// Indexed by stars - 1
	public float[] MiningBonus = { 1.25f, 1.5f, 1.75f, 2f, 2.25f, 2.5f, 2.95f };
	public float[] ShipBonus = { 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 4.9f };
	// Secondary skills start at 3 stars, 1 means no bonus
	public float[] AllMiningBonus = { 1f, 1f, 1.05f, 1.1f, 1.2f, 1.3f, 1.5f };
	public float[] AllShipBonus = { 1f, 1f, 1.1f, 1.2f, 1.4f, 1.6f, 2f };
	public float[] AllProductionBonus = { 1f, 1f, 1.05f, 1.1f, 1.2f, 1.4f, 1.7f };
}

[System.Serializable]
public class ManagerSchoolSetting
{
	public ManagerSchool School;
	public string Name;
	public int Cost;
	public int[] Stars;
	public float[] Chances;
}

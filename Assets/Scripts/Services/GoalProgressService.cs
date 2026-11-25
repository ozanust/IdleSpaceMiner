/// <summary>
/// Describes and measures the goals used by tasks and achievements. Goals check game state,
/// so only claims need to be saved.
/// </summary>
public class GoalProgressService : IGoalProgressService
{
	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly ResourceSettings resourceSettings;
	private readonly IManagerController managerController;

	public GoalProgressService(IPlayerModel playerModel, ISpaceModel spaceModel, ResourceSettings resourceSettings, IManagerController managerController)
	{
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;
		this.resourceSettings = resourceSettings;
		this.managerController = managerController;
	}

	public bool IsCompleted(TaskSetting goal)
	{
		return GetProgress(goal) >= goal.TargetAmount;
	}

	public float GetProgressRatio(TaskSetting goal)
	{
		return UnityEngine.Mathf.Clamp01((float)GetProgress(goal) / goal.TargetAmount);
	}

	public string GetProgressText(TaskSetting goal)
	{
		long progress = System.Math.Min(GetProgress(goal), goal.TargetAmount);
		return $"{NumberFormatter.Format(progress)} / {NumberFormatter.Format(goal.TargetAmount)}";
	}

	public string GetDescription(TaskSetting goal)
	{
		switch (goal.Type)
		{
			case TaskType.UpgradePlanets:
				return $"Upgrade planets {goal.TargetAmount} times";
			case TaskType.HaveResource:
				return $"Collect {NumberFormatter.Format(goal.TargetAmount)} {GetResourceName(goal.Resource)}";
			case TaskType.UnlockPlanets:
				return $"Unlock {goal.TargetAmount} planets";
			case TaskType.Research:
				return $"Research '{resourceSettings.GetResearchSetting(goal.Research).Name}'";
			case TaskType.HaveSmelters:
				return $"Build {goal.TargetAmount} smelters";
			case TaskType.HaveMoney:
				return $"Have {NumberFormatter.FormatMoney(goal.TargetAmount)}";
			case TaskType.DestroyAsteroids:
				return goal.TargetAmount == 1 ? "Mine an asteroid" : $"Mine {goal.TargetAmount} asteroids";
			case TaskType.HaveCrafters:
				return $"Build {goal.TargetAmount} crafters";
			case TaskType.ResearchCount:
				return $"Complete {goal.TargetAmount} researches";
			case TaskType.HireManagers:
				return goal.TargetAmount == 1 ? "Hire a manager" : $"Hire {goal.TargetAmount} managers";
			default:
				return goal.Type.ToString();
		}
	}

	private long GetProgress(TaskSetting goal)
	{
		switch (goal.Type)
		{
			case TaskType.UpgradePlanets:
				return GetPlanetUpgradeCount();
			case TaskType.HaveResource:
				return playerModel.GetResource(goal.Resource);
			case TaskType.UnlockPlanets:
				return GetUnlockedPlanetCount();
			case TaskType.Research:
				return playerModel.IsResearchUnlocked(goal.Research) ? goal.TargetAmount : 0;
			case TaskType.HaveSmelters:
				// Smelter 0 comes with the Smelter research, ids are unlocked in order
				return playerModel.IsResearchUnlocked(ResearchType.Smelter) ? playerModel.GetLastUnlockedSmelterId() + 1 : 0;
			case TaskType.HaveMoney:
				return playerModel.GetMoney();
			case TaskType.DestroyAsteroids:
				return playerModel.AsteroidsDestroyed;
			case TaskType.HaveCrafters:
				// Crafter 0 comes with the Crafter research, ids are unlocked in order
				return playerModel.IsResearchUnlocked(ResearchType.Crafter) ? playerModel.GetLastUnlockedCrafterId() + 1 : 0;
			case TaskType.ResearchCount:
				return playerModel.GetUnlockedResearchs().Length;
			case TaskType.HireManagers:
				return managerController.TotalHired;
			default:
				return 0;
		}
	}

	private int GetUnlockedPlanetCount()
	{
		int count = 0;
		PlanetData[] planets = spaceModel.GetPlanetsData();
		if (planets == null)
		{
			return 0;
		}

		foreach (PlanetData planet in planets)
		{
			if (planet.IsUnlocked)
			{
				count++;
			}
		}

		return count;
	}

	private int GetPlanetUpgradeCount()
	{
		int count = 0;
		PlanetData[] planets = spaceModel.GetPlanetsData();
		if (planets == null)
		{
			return 0;
		}

		foreach (PlanetData planet in planets)
		{
			if (planet.IsUnlocked)
			{
				// Each stat starts at level 1 when the planet is unlocked
				count += planet.MiningRateLevel + planet.ShipSpeedLevel + planet.ShipCargoLevel - 3;
			}
		}

		return count;
	}

	private string GetResourceName(ResourceType type)
	{
		ResourceDataSetting data = resourceSettings.GetResourceData(type);
		return data != null ? data.Name : type.ToString();
	}
}

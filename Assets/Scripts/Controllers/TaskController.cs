using UnityEngine;
using Zenject;

/// <summary>
/// Guided task chain that doubles as the tutorial: one active task at a time,
/// each paying a cash reward when claimed. Tasks check game state, so no progress counters are saved.
/// </summary>
public class TaskController : ITaskController, IInitializable, ITickable
{
	private const float CheckInterval = 0.5f;

	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly ISaveService saveService;
	private readonly GameSettings gameSettings;
	private readonly ResourceSettings resourceSettings;
	private readonly SignalBus signalBus;

	private float checkTimer;

	public int CurrentTaskIndex { get; private set; }
	public bool AreAllTasksCompleted => CurrentTaskIndex >= gameSettings.Tasks.Length;
	public bool IsCurrentTaskCompleted => !AreAllTasksCompleted && GetProgress(CurrentTask) >= CurrentTask.TargetAmount;

	private TaskSetting CurrentTask => gameSettings.Tasks[CurrentTaskIndex];

	public TaskController(
		IPlayerModel playerModel,
		ISpaceModel spaceModel,
		ISaveService saveService,
		GameSettings gameSettings,
		ResourceSettings resourceSettings,
		SignalBus signalBus)
	{
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;
		this.saveService = saveService;
		this.gameSettings = gameSettings;
		this.resourceSettings = resourceSettings;
		this.signalBus = signalBus;
	}

	public void Initialize()
	{
		SaveData save = saveService.Load();
		if (save != null)
		{
			CurrentTaskIndex = Mathf.Clamp(save.CurrentTaskIndex, 0, gameSettings.Tasks.Length);
		}
	}

	public void Tick()
	{
		checkTimer += Time.deltaTime;
		if (checkTimer < CheckInterval)
		{
			return;
		}

		checkTimer = 0;

		// Task progress depends on many systems, so views just refresh periodically
		signalBus.Fire<TaskUpdatedSignal>();
	}

	public void ClaimCurrentTask()
	{
		if (!IsCurrentTaskCompleted)
		{
			return;
		}

		playerModel.AddMoney(CurrentTask.Reward);
		CurrentTaskIndex++;
		signalBus.Fire<TaskUpdatedSignal>();
	}

	public long GetCurrentTaskReward()
	{
		return AreAllTasksCompleted ? 0 : CurrentTask.Reward;
	}

	public string GetCurrentTaskDescription()
	{
		if (AreAllTasksCompleted)
		{
			return "All tasks completed!";
		}

		TaskSetting task = CurrentTask;
		switch (task.Type)
		{
			case TaskType.UpgradePlanets:
				return $"Upgrade planets {task.TargetAmount} times";
			case TaskType.HaveResource:
				return $"Collect {NumberFormatter.Format(task.TargetAmount)} {GetResourceName(task.Resource)}";
			case TaskType.UnlockPlanets:
				return $"Unlock {task.TargetAmount} planets";
			case TaskType.Research:
				return $"Research '{resourceSettings.GetResearchSetting(task.Research).Name}'";
			case TaskType.HaveSmelters:
				return $"Build {task.TargetAmount} smelters";
			case TaskType.HaveMoney:
				return $"Have {NumberFormatter.FormatMoney(task.TargetAmount)}";
			default:
				return task.Type.ToString();
		}
	}

	public string GetCurrentTaskProgress()
	{
		if (AreAllTasksCompleted)
		{
			return "";
		}

		long progress = System.Math.Min(GetProgress(CurrentTask), CurrentTask.TargetAmount);
		return $"{NumberFormatter.Format(progress)} / {NumberFormatter.Format(CurrentTask.TargetAmount)}";
	}

	private long GetProgress(TaskSetting task)
	{
		switch (task.Type)
		{
			case TaskType.UpgradePlanets:
				return GetPlanetUpgradeCount();
			case TaskType.HaveResource:
				return playerModel.GetResource(task.Resource);
			case TaskType.UnlockPlanets:
				return GetUnlockedPlanetCount();
			case TaskType.Research:
				return playerModel.IsResearchUnlocked(task.Research) ? task.TargetAmount : 0;
			case TaskType.HaveSmelters:
				// Smelter 0 comes with the Smelter research, ids are unlocked in order
				return playerModel.IsResearchUnlocked(ResearchType.Smelter) ? playerModel.GetLastUnlockedSmelterId() + 1 : 0;
			case TaskType.HaveMoney:
				return playerModel.GetMoney();
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

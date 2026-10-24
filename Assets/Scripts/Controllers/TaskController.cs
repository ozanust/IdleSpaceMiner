using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Task list that doubles as the tutorial: the first few unclaimed tasks are active at once,
/// each paying a cash reward when claimed. Tasks check game state, so only claims are saved.
/// </summary>
public class TaskController : ITaskController, IInitializable, ITickable
{
	private const float CheckInterval = 0.5f;
	private const int MaxActiveTasks = 4;

	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly ISaveService saveService;
	private readonly GameSettings gameSettings;
	private readonly ResourceSettings resourceSettings;
	private readonly SignalBus signalBus;

	private readonly HashSet<int> claimedTasks = new HashSet<int>();
	private readonly List<int> activeTasks = new List<int>();
	private float checkTimer;

	public bool AreAllTasksCompleted => claimedTasks.Count >= gameSettings.Tasks.Length;

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
		if (save != null && save.ClaimedTasks != null)
		{
			foreach (int index in save.ClaimedTasks)
			{
				if (index >= 0 && index < gameSettings.Tasks.Length)
				{
					claimedTasks.Add(index);
				}
			}
		}

		UpdateActiveTasks();
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

	public IReadOnlyList<int> GetActiveTasks()
	{
		return activeTasks;
	}

	public int[] GetClaimedTasks()
	{
		int[] result = new int[claimedTasks.Count];
		claimedTasks.CopyTo(result);
		return result;
	}

	public bool HasClaimableTask()
	{
		foreach (int index in activeTasks)
		{
			if (IsTaskCompleted(index))
			{
				return true;
			}
		}

		return false;
	}

	public bool IsTaskCompleted(int index)
	{
		TaskSetting task = gameSettings.Tasks[index];
		return GetProgress(task) >= task.TargetAmount;
	}

	public void ClaimTask(int index)
	{
		if (!activeTasks.Contains(index) || !IsTaskCompleted(index))
		{
			return;
		}

		claimedTasks.Add(index);
		playerModel.AddMoney(gameSettings.Tasks[index].Reward);
		UpdateActiveTasks();
		signalBus.Fire<TaskUpdatedSignal>();
	}

	public long GetTaskReward(int index)
	{
		return gameSettings.Tasks[index].Reward;
	}

	public string GetTaskDescription(int index)
	{
		TaskSetting task = gameSettings.Tasks[index];
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

	public string GetTaskProgressText(int index)
	{
		TaskSetting task = gameSettings.Tasks[index];
		long progress = System.Math.Min(GetProgress(task), task.TargetAmount);
		return $"{NumberFormatter.Format(progress)} / {NumberFormatter.Format(task.TargetAmount)}";
	}

	public float GetTaskProgressRatio(int index)
	{
		TaskSetting task = gameSettings.Tasks[index];
		return Mathf.Clamp01((float)GetProgress(task) / task.TargetAmount);
	}

	/// <summary>Active tasks are the first unclaimed ones, in the order they are defined.</summary>
	private void UpdateActiveTasks()
	{
		activeTasks.Clear();
		for (int i = 0; i < gameSettings.Tasks.Length && activeTasks.Count < MaxActiveTasks; i++)
		{
			if (!claimedTasks.Contains(i))
			{
				activeTasks.Add(i);
			}
		}
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

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
	private readonly ISaveService saveService;
	private readonly GameSettings gameSettings;
	private readonly IGoalProgressService goals;
	private readonly SignalBus signalBus;

	private readonly HashSet<int> claimedTasks = new HashSet<int>();
	private readonly List<int> activeTasks = new List<int>();
	private float checkTimer;

	public bool AreAllTasksCompleted => claimedTasks.Count >= gameSettings.Tasks.Length;

	public TaskController(
		IPlayerModel playerModel,
		ISaveService saveService,
		GameSettings gameSettings,
		IGoalProgressService goals,
		SignalBus signalBus)
	{
		this.playerModel = playerModel;
		this.saveService = saveService;
		this.gameSettings = gameSettings;
		this.goals = goals;
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

	public IReadOnlyList<int> GetVisibleEntries()
	{
		return activeTasks;
	}

	public int[] GetClaimedTasks()
	{
		int[] result = new int[claimedTasks.Count];
		claimedTasks.CopyTo(result);
		return result;
	}

	public bool HasClaimableEntry()
	{
		foreach (int index in activeTasks)
		{
			if (IsCompleted(index))
			{
				return true;
			}
		}

		return false;
	}

	public bool IsCompleted(int index)
	{
		return goals.IsCompleted(gameSettings.Tasks[index]);
	}

	// Claimed tasks leave the list, so a visible task is never claimed
	public bool IsClaimed(int index)
	{
		return claimedTasks.Contains(index);
	}

	public void Claim(int index)
	{
		if (!activeTasks.Contains(index) || !IsCompleted(index))
		{
			return;
		}

		claimedTasks.Add(index);
		playerModel.AddMoney(gameSettings.Tasks[index].Reward);
		UpdateActiveTasks();
		signalBus.Fire<TaskUpdatedSignal>();
	}

	public string GetDescription(int index)
	{
		return goals.GetDescription(gameSettings.Tasks[index]);
	}

	public string GetRewardText(int index)
	{
		return "Reward: " + NumberFormatter.FormatMoney(gameSettings.Tasks[index].Reward);
	}

	public string GetProgressText(int index)
	{
		return goals.GetProgressText(gameSettings.Tasks[index]);
	}

	public float GetProgressRatio(int index)
	{
		return goals.GetProgressRatio(gameSettings.Tasks[index]);
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
}

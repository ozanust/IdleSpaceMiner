using System.Collections.Generic;
using Zenject;

/// <summary>
/// Permanent milestones paying Dark Matter, all listed at once. Claimed achievements stay in the
/// list; the order puts claimable ones first, then unfinished, then claimed.
/// </summary>
public class AchievementController : IAchievementController, IInitializable
{
	private readonly IPlayerModel playerModel;
	private readonly ISaveService saveService;
	private readonly GameSettings gameSettings;
	private readonly IGoalProgressService goals;
	private readonly SignalBus signalBus;

	private readonly HashSet<int> claimed = new HashSet<int>();
	private readonly List<int> visible = new List<int>();

	public AchievementController(IPlayerModel playerModel, ISaveService saveService, GameSettings gameSettings, IGoalProgressService goals, SignalBus signalBus)
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
		if (save != null && save.ClaimedAchievements != null)
		{
			foreach (int index in save.ClaimedAchievements)
			{
				if (index >= 0 && index < gameSettings.Achievements.Length)
				{
					claimed.Add(index);
				}
			}
		}
	}

	public IReadOnlyList<int> GetVisibleEntries()
	{
		visible.Clear();
		AddWhere(i => !claimed.Contains(i) && IsCompleted(i));
		AddWhere(i => !claimed.Contains(i) && !IsCompleted(i));
		AddWhere(i => claimed.Contains(i));
		return visible;
	}

	private void AddWhere(System.Predicate<int> predicate)
	{
		for (int i = 0; i < gameSettings.Achievements.Length; i++)
		{
			if (predicate(i))
			{
				visible.Add(i);
			}
		}
	}

	public int[] GetClaimedAchievements()
	{
		int[] result = new int[claimed.Count];
		claimed.CopyTo(result);
		return result;
	}

	public bool HasClaimableEntry()
	{
		for (int i = 0; i < gameSettings.Achievements.Length; i++)
		{
			if (!claimed.Contains(i) && IsCompleted(i))
			{
				return true;
			}
		}

		return false;
	}

	public bool IsCompleted(int index)
	{
		return goals.IsCompleted(gameSettings.Achievements[index]);
	}

	public bool IsClaimed(int index)
	{
		return claimed.Contains(index);
	}

	public void Claim(int index)
	{
		if (claimed.Contains(index) || !IsCompleted(index))
		{
			return;
		}

		claimed.Add(index);
		playerModel.AddCurrency(CurrencyType.DarkMatter, (int)gameSettings.Achievements[index].Reward);
		signalBus.Fire<TaskUpdatedSignal>();
	}

	public string GetDescription(int index)
	{
		return goals.GetDescription(gameSettings.Achievements[index]);
	}

	public string GetRewardText(int index)
	{
		return $"Reward: {gameSettings.Achievements[index].Reward} Dark Matter";
	}

	public string GetProgressText(int index)
	{
		return goals.GetProgressText(gameSettings.Achievements[index]);
	}

	public float GetProgressRatio(int index)
	{
		return goals.GetProgressRatio(gameSettings.Achievements[index]);
	}
}

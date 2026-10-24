using System;
using UnityEngine;
using Zenject;

/// <summary>
/// Grants progress for time the game was not running: after loading a save, and after
/// the app is resumed from the background (detected as a long gap between ticks).
/// Mining runs at a reduced rate and its ore goes straight to the mothership;
/// smelters and crafters run at full speed.
/// </summary>
public class OfflineProgressController : IOfflineProgressController, ITickable
{
	// A gap between two frames longer than this means the app was suspended
	private const double SuspendThresholdSeconds = 5;

	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly IProductionController productionController;
	private readonly PlanetSettings planetSettings;
	private readonly GameSettings gameSettings;

	// Null until the first tick, so slow scene loading isn't counted as offline time
	private DateTime? lastTickUtc;

	public OfflineProgressController(
		IPlayerModel playerModel,
		ISpaceModel spaceModel,
		IProductionController productionController,
		PlanetSettings planetSettings,
		GameSettings gameSettings)
	{
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;
		this.productionController = productionController;
		this.planetSettings = planetSettings;
		this.gameSettings = gameSettings;
	}

	public void ApplySinceSave(SaveData save)
	{
		if (save.SavedAtUtcTicks <= 0)
		{
			return;
		}

		ApplyOfflineProgress((DateTime.UtcNow - new DateTime(save.SavedAtUtcTicks, DateTimeKind.Utc)).TotalSeconds);
	}

	public void Tick()
	{
		DateTime now = DateTime.UtcNow;
		DateTime? previous = lastTickUtc;
		lastTickUtc = now;

		if (!previous.HasValue)
		{
			return;
		}

		double gap = (now - previous.Value).TotalSeconds;
		if (gap > SuspendThresholdSeconds)
		{
			ApplyOfflineProgress(gap);
		}
	}

	private void ApplyOfflineProgress(double elapsedSeconds)
	{
		float seconds = (float)Math.Min(elapsedSeconds, gameSettings.GlobalSettings.MaxOfflineSeconds);
		if (seconds <= 0)
		{
			return;
		}

		MineOffline(seconds);
		productionController.SimulateOffline(seconds);

		Debug.Log($"[OfflineProgress] Applied {seconds:F0}s of offline progress (away {elapsedSeconds:F0}s)");
	}

	private void MineOffline(float seconds)
	{
		PlanetData[] planets = spaceModel.GetPlanetsData();
		if (planets == null)
		{
			return;
		}

		float miningSeconds = seconds * gameSettings.GlobalSettings.OfflineMiningMultiplier;

		foreach (PlanetData planet in planets)
		{
			PlanetDataSetting setting;
			if (!planet.IsUnlocked || !planetSettings.TryGetPlanetSetting(planet.PlanetIndex, out setting))
			{
				continue;
			}

			foreach (PlanetMiningYield yield in setting.MiningYieldRatios)
			{
				int amount = Mathf.FloorToInt(planet.CurrentTotalMiningRate * yield.YieldRatio * miningSeconds);
				if (amount > 0)
				{
					playerModel.AddResource(yield.Type, amount);
				}
			}
		}
	}
}

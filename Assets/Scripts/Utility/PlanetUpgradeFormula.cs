using UnityEngine;

/// <summary>
/// Planet upgrade curves. Stats grow quadratically while prices grow exponentially,
/// so every upgrade takes ~1.2x longer to pay back than the previous one.
/// Levels start at 1 when a planet is unlocked.
/// </summary>
public static class PlanetUpgradeFormula
{
	// Upgrade price at level 1 is the planet's unlock price divided by this
	private const float BaseUpgradePriceDivider = 20f;

	public static float GetMiningRate(PlanetDataSetting setting, int level)
	{
		int n = level - 1;
		return setting.TotalStartingMiningRate * (1f + 0.4f * n + 0.068f * n * n);
	}

	public static float GetShipSpeed(PlanetDataSetting setting, int level)
	{
		int n = level - 1;
		return setting.StartingShipSpeed * (1f + 0.2f * n + n * n / 75f);
	}

	public static int GetShipCargo(PlanetDataSetting setting, int level)
	{
		int n = level - 1;
		return Mathf.RoundToInt(setting.StartingShipCargo * (1f + 0.4f * n + 0.02f * n * n));
	}

	/// <summary>Price to upgrade from the given level to the next one.</summary>
	public static long GetUpgradePrice(PlanetDataSetting setting, int level)
	{
		return (long)System.Math.Round(setting.PlanetPrice / BaseUpgradePriceDivider * System.Math.Pow(setting.PriceIncreaseMultiplier, level - 1));
	}
}

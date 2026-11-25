using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SpaceModel : ISpaceModel
{
	private PlanetData[] planetData;
	private Dictionary<int, Transform> planetTransformDatas = new Dictionary<int, Transform>();
	private Dictionary<int, AsteroidData> asteroidRewardsData = new Dictionary<int, AsteroidData>();
	readonly SignalBus signalBus;
	private PlanetSettings planetSettings;

	public SpaceModel(SignalBus signalBus, PlanetSettings planetSettings)
	{
		this.signalBus = signalBus;
		this.planetSettings = planetSettings;

		signalBus.Subscribe<PlanetTransformSignal>(OnSetPlanetTransform);
	}

    public PlanetData[] GetPlanetsData()
	{
		return planetData;
	}

	public void InitializePlanetData(PlanetData[] planetData)
	{
		this.planetData = planetData;
		signalBus.Fire(new SpaceModelInitializedSignal() { Data = planetData });
	}

	public void UnlockPlanet(int planetId)
	{
		planetData[planetId].IsUnlocked = true;
		PlanetData pd = planetData[planetId];
		PlanetDataSetting pds;

		if (planetSettings.TryGetPlanetSetting(planetId, out pds))
		{
			pd.MiningRateLevel += 1;
			pd.CurrentTotalMiningRate = PlanetUpgradeFormula.GetMiningRate(pds, pd.MiningRateLevel);
			pd.TotalMiningRateUpdatePrice = PlanetUpgradeFormula.GetUpgradePrice(pds, pd.MiningRateLevel);
			pd.ShipSpeedLevel += 1;
			pd.CurrentShipSpeed = PlanetUpgradeFormula.GetShipSpeed(pds, pd.ShipSpeedLevel);
			pd.ShipSpeedUpdatePrice = PlanetUpgradeFormula.GetUpgradePrice(pds, pd.ShipSpeedLevel);
			pd.ShipCargoLevel += 1;
			pd.CurrentShipCargo = PlanetUpgradeFormula.GetShipCargo(pds, pd.ShipCargoLevel);
			pd.ShipCargoUpdatePrice = PlanetUpgradeFormula.GetUpgradePrice(pds, pd.ShipCargoLevel);
		}

		signalBus.Fire(new PlanetUnlockedSignal() { PlanetId = planetId });
	}

	public void UnravelPlanet(int planetIndex)
	{
		planetData[planetIndex].IsUnraveled = true;
		signalBus.Fire(new PlanetUnraveledSignal() { PlanetId = planetIndex });
	}

	private void OnSetPlanetTransform(PlanetTransformSignal signal)
	{
		if (!planetTransformDatas.ContainsKey(signal.PlanetId))
		{
			planetTransformDatas.Add(signal.PlanetId, signal.PlanetTransform);
		}
	}

	public bool TryGetPlanetTransform(int planetId, out Transform transform)
	{
		transform = null;
		if (!planetTransformDatas.ContainsKey(planetId))
		{
			Debug.LogWarning("Couldn't find planet transform!");
			return false;
		}

		transform = planetTransformDatas[planetId];
		return true;
	}

	public bool TryGetPlanetData(int planetId, out PlanetData data)
	{
		data = null;
		for (int i = 0; i < planetData.Length; i++)
		{
			if (planetData[i].PlanetIndex == planetId)
			{
				data = planetData[i];
				return true;
			}
		}

		return false;
	}

	public void UpdatePlanetMiningRate(int planetId)
	{
		if (planetData[planetId].IsUnlocked)
		{
			PlanetData pd = planetData[planetId];
			PlanetDataSetting pds;

			if (planetSettings.TryGetPlanetSetting(planetId, out pds))
			{
				pd.MiningRateLevel += 1;
				pd.CurrentTotalMiningRate = PlanetUpgradeFormula.GetMiningRate(pds, pd.MiningRateLevel);
				pd.TotalMiningRateUpdatePrice = PlanetUpgradeFormula.GetUpgradePrice(pds, pd.MiningRateLevel);
				signalBus.Fire(new PlanetUpdatedSignal() { PlanetId = planetId });
			}
		}
	}

	public void UpdatePlanetShipSpeed(int planetId)
	{
		if (planetData[planetId].IsUnlocked)
		{
			PlanetData pd = planetData[planetId];
			PlanetDataSetting pds;

			if (planetSettings.TryGetPlanetSetting(planetId, out pds))
			{
				pd.ShipSpeedLevel += 1;
				pd.CurrentShipSpeed = PlanetUpgradeFormula.GetShipSpeed(pds, pd.ShipSpeedLevel);
				pd.ShipSpeedUpdatePrice = PlanetUpgradeFormula.GetUpgradePrice(pds, pd.ShipSpeedLevel);
				signalBus.Fire(new PlanetUpdatedSignal() { PlanetId = planetId });
			}
		}
	}

	public void UpdatePlanetCargo(int planetId)
	{
		if (planetData[planetId].IsUnlocked)
		{
			PlanetData pd = planetData[planetId];
			PlanetDataSetting pds;

			if (planetSettings.TryGetPlanetSetting(planetId, out pds))
			{
				pd.ShipCargoLevel += 1;
				pd.CurrentShipCargo = PlanetUpgradeFormula.GetShipCargo(pds, pd.ShipCargoLevel);
				pd.ShipCargoUpdatePrice = PlanetUpgradeFormula.GetUpgradePrice(pds, pd.ShipCargoLevel);
				signalBus.Fire(new PlanetUpdatedSignal() { PlanetId = planetId });
			}
		}
	}

	public void AddAsteroidData(AsteroidData data, int id)
	{
		if (!asteroidRewardsData.ContainsKey(id))
		{
			asteroidRewardsData.Add(id, data);
		}
		else
		{
			asteroidRewardsData[id] = data;
		}
	}

	public AsteroidData GetAsteroidRewards(int id)
	{
		return asteroidRewardsData[id];
	}

	public void RemoveAsteroidData(int id)
	{
		if (asteroidRewardsData.ContainsKey(id))
		{
			asteroidRewardsData.Remove(id);
		}
	}
}

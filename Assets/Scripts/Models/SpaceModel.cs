using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SpaceModel : ISpaceModel
{
	private PlanetData[] planetData;
	private Dictionary<int, Transform> planetTransformDatas = new Dictionary<int, Transform>();
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
			pd.CurrentTotalMiningRate = pds.TotalStartingMiningRate;
			pd.TotalMiningRateUpdatePrice = pds.StartingMiningRatePrice;
			pd.ShipSpeedLevel += 1;
			pd.CurrentShipSpeed = pds.StartingShipSpeed;
			pd.ShipSpeedUpdatePrice = pds.StartingShipSpeedPrice;
			pd.ShipCargoLevel += 1;
			pd.CurrentShipCargo = pds.StartingShipCargo;
			pd.ShipCargoUpdatePrice = pds.StartingShipCargoPrice;
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
				pd.CurrentTotalMiningRate = pds.TotalStartingMiningRate * Mathf.Pow(pds.MiningRateIncreaseMultiplier, pd.MiningRateLevel);
				pd.TotalMiningRateUpdatePrice = Mathf.RoundToInt(pds.StartingMiningRatePrice * Mathf.Pow(pds.PriceIncreaseMultiplier, pd.MiningRateLevel));
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
				pd.CurrentShipSpeed = pds.StartingShipSpeed * Mathf.Pow(pds.ShipSpeedIncreaseMultiplier, pd.ShipSpeedLevel);
				pd.ShipSpeedUpdatePrice = Mathf.RoundToInt(pds.StartingShipSpeedPrice * Mathf.Pow(pds.PriceIncreaseMultiplier, pd.ShipSpeedLevel));
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
				pd.CurrentShipCargo = Mathf.RoundToInt(pds.StartingShipCargo * Mathf.Pow(pds.CargoIncreaseMultiplier, pd.ShipCargoLevel));
				pd.ShipCargoUpdatePrice = Mathf.RoundToInt(pds.StartingShipCargoPrice * Mathf.Pow(pds.PriceIncreaseMultiplier, pd.ShipCargoLevel));
				signalBus.Fire(new PlanetUpdatedSignal() { PlanetId = planetId });
			}
		}
	}
}

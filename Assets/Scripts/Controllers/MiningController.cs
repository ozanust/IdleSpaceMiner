using Zenject;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningController : IMiningController, ITickable, IDisposable
{
	private PlanetSettings planetSettings;
	private IPlayerModel playerModel;
	private ISpaceModel spaceModel;
	private SignalBus signalBus;
	private Dictionary<int, MiningData> mineData = new Dictionary<int, MiningData>();
	private Dictionary<int, TransferResourcesData[]> transferData = new Dictionary<int, TransferResourcesData[]>();
	private ResourceSettings resourceSettings;
	private IManagerController managerController;
	private IMothershipController mothershipController;

	// Rising icon and "+12" where a cargo ship unloads at the mothership
	private const float DeliveryTextSize = 12f;
	private const float DeliveryIconSize = 1.1f;
	private const float DeliveryRowHeight = 1.35f;
	private static readonly Vector3 DeliveryTextOffset = new Vector3(0f, 0.6f, 0f);
	private static readonly Color DeliveryTextColor = new Color(1f, 0.88f, 0.55f);

	// Management research bonus for all planets
	private float researchMultiplier = 1f;

	public MiningController(SignalBus signalBus, PlanetSettings planetSettings, ResourceSettings resourceSettings, IPlayerModel playerModel, ISpaceModel spaceModel, IManagerController managerController, IMothershipController mothershipController)
	{
		this.mothershipController = mothershipController;
		this.managerController = managerController;
		this.signalBus = signalBus;
		this.planetSettings = planetSettings;
		this.resourceSettings = resourceSettings;
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;

		this.signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
		this.signalBus.Subscribe<SpaceModelInitializedSignal>(OnSpaceModelInitialized);
		this.signalBus.Subscribe<PlanetUnlockedSignal>(OnPlanetUnlocked);
		this.signalBus.Subscribe<CargoShipPlanetArrivalSignal>(OnCargoArrivedPlanet);
		this.signalBus.Subscribe<CargoShipMothershipArrivalSignal>(OnCargoArrivedMothership);
		this.signalBus.Subscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
	}

	/// <summary>Unsubscribes from all signals.</summary>
	public void Dispose()
	{
		signalBus.Unsubscribe<ResearchCompletedSignal>(OnResearchCompleted);
		signalBus.Unsubscribe<SpaceModelInitializedSignal>(OnSpaceModelInitialized);
		signalBus.Unsubscribe<PlanetUnlockedSignal>(OnPlanetUnlocked);
		signalBus.Unsubscribe<CargoShipPlanetArrivalSignal>(OnCargoArrivedPlanet);
		signalBus.Unsubscribe<CargoShipMothershipArrivalSignal>(OnCargoArrivedMothership);
		signalBus.Unsubscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
	}

	/// <summary>
	/// Pre-populates mining data for planets that are already unlocked when
	/// loading from a save, so the cargo loop and planet info UI work immediately.
	/// </summary>
	private void OnSpaceModelInitialized(SpaceModelInitializedSignal signal)
	{
		foreach (PlanetData planet in signal.Data)
		{
			if (planet.IsUnlocked && !mineData.ContainsKey(planet.PlanetIndex))
			{
				RegisterMiningData(planet.PlanetIndex, planet.CurrentTotalMiningRate);
			}
		}
	}

	public void Tick()
	{
		ProduceResources();
	}

	private void OnPlanetUnlocked(PlanetUnlockedSignal signal)
	{
		PlanetDataSetting planetDataSettings;
		if (planetSettings.TryGetPlanetSetting(signal.PlanetId, out planetDataSettings))
		{
			RegisterMiningData(signal.PlanetId, planetDataSettings.TotalStartingMiningRate);
		}
	}

	/// <summary>Adds a new MiningData entry for the given planet if one does not already exist.</summary>
	private void RegisterMiningData(int planetId, float totalMineRate)
	{
		if (mineData.ContainsKey(planetId))
		{
			return;
		}

		PlanetDataSetting planetDataSettings;
		if (planetSettings.TryGetPlanetSetting(planetId, out planetDataSettings))
		{
			MiningData md = new MiningData(totalMineRate, planetDataSettings.MiningYieldRatios);
			mineData.Add(planetId, md);
		}
	}

	private void OnPlanetUpdated(PlanetUpdatedSignal signal)
	{
		PlanetData planetData;
		if (spaceModel.TryGetPlanetData(signal.PlanetId, out planetData))
		{
			mineData[signal.PlanetId].TotalMineRate = planetData.CurrentTotalMiningRate;
		}
	}

	private void OnCargoArrivedPlanet(CargoShipPlanetArrivalSignal signal)
	{
		if (!transferData.ContainsKey(signal.PlanetId))
		{
			transferData.Add(signal.PlanetId, TransferResources(signal.PlanetId, signal.CargoCapacity));
		}
		else
		{
			transferData[signal.PlanetId] = TransferResources(signal.PlanetId, signal.CargoCapacity);
		}
	}

	private void OnCargoArrivedMothership(CargoShipMothershipArrivalSignal signal)
	{
		TransferResourcesData[] resourcesData;
		if (!transferData.TryGetValue(signal.PlanetId, out resourcesData))
		{
			return;
		}

		// Delivered cargo is no longer in transit, or saves would count it twice
		transferData.Remove(signal.PlanetId);

		// One icon and amount row per resource, stacked upwards
		int row = 0;
		for (int i = 0; i < resourcesData.Length; i++)
		{
			if (resourcesData[i].Amount <= 0)
			{
				continue;
			}

			playerModel.AddResource(resourcesData[i].Type, resourcesData[i].Amount);

			ResourceDataSetting resource = resourceSettings.GetResourceData(resourcesData[i].Type);
			Vector3 position = signal.Position + DeliveryTextOffset + Vector3.up * (row * DeliveryRowHeight);
			FloatingTextView.SpawnWithIcon(resource != null ? resource.Icon : null, $"+{NumberFormatter.Format(resourcesData[i].Amount)}",
				position, DeliveryTextColor, DeliveryTextSize, DeliveryIconSize);
			row++;
		}
	}

	private void ProduceResources()
	{
		if (mineData.Count <= 0)
		{
			return;
		}

		foreach (KeyValuePair<int, MiningData> planet in mineData)
		{
			MiningData md = planet.Value;
			float multiplier = GetMiningMultiplier(planet.Key);
			for (int i = 0; i < md.MineDatas.Length; i++)
			{
				md.MineDatas[i].MinedAmount += md.TotalMineRate * multiplier * md.MineDatas[i].Ratio * Time.deltaTime;
			}
		}
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == ResearchType.Management)
		{
			researchMultiplier = 1.1f;
		}
	}

	public float GetMiningMultiplier(int planetId)
	{
		return researchMultiplier * managerController.GetMiningMultiplier(planetId) * mothershipController.GetMultiplier(RoomType.Engineering);
	}

	public float GetValuePerSecond()
	{
		float total = 0f;
		foreach (KeyValuePair<int, MiningData> planet in mineData)
		{
			MiningData md = planet.Value;
			foreach (ResourceMiningData resource in md.MineDatas)
			{
				total += md.TotalMineRate * GetMiningMultiplier(planet.Key) * resource.Ratio * resourceSettings.GetResourceValue(resource.Type);
			}
		}

		return total;
	}

	public TransferResourcesData[] TransferResources(int planetId, int amount)
	{
		List<TransferResourcesData> transferData = new List<TransferResourcesData>();

		for (int i = 0; i < mineData[planetId].MineDatas.Length; i++)
		{
			if (amount <= 0)
			{
				break;
			}
			
			if (amount > mineData[planetId].MineDatas[i].MinedAmount)
			{
				TransferResourcesData tempData = new TransferResourcesData(mineData[planetId].MineDatas[i].Type, Mathf.FloorToInt(mineData[planetId].MineDatas[i].MinedAmount));
				transferData.Add(tempData);

				// This can create errors if round amount is zero
				amount -= Mathf.FloorToInt(mineData[planetId].MineDatas[i].MinedAmount);
				mineData[planetId].MineDatas[i].MinedAmount = 0;
			}
			else if(amount > 0)
			{
				TransferResourcesData tempData = new TransferResourcesData(mineData[planetId].MineDatas[i].Type, amount);
				transferData.Add(tempData);
				mineData[planetId].MineDatas[i].MinedAmount -= amount;
			}
		}

		return transferData.ToArray();
	}

	public bool TryGetMiningData(int planetId, out MiningData data)
	{
		data = null;
		if (mineData.ContainsKey(planetId))
		{
			data = mineData[planetId];
			return true;
		}

		return false;
	}

	/// <summary>
	/// Aggregates all resources currently in transit across all cargo ships into a single dictionary.
	/// Called during save to fold in-transit cargo into the centralized mothership snapshot.
	/// </summary>
	public Dictionary<ResourceType, int> GetTransferData()
	{
		Dictionary<ResourceType, int> result = new Dictionary<ResourceType, int>();

		foreach (TransferResourcesData[] entries in transferData.Values)
		{
			foreach (TransferResourcesData entry in entries)
			{
				if (result.ContainsKey(entry.Type))
				{
					result[entry.Type] += entry.Amount;
				}
				else
				{
					result[entry.Type] = entry.Amount;
				}
			}
		}

		return result;
	}
}

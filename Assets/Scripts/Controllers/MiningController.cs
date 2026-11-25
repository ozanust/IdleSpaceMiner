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
	private IBoostController boostController;

	// Rising icon and "+12" where a cargo ship unloads at the mothership
	private const float DeliveryTextSize = 12f;
	private const float DeliveryIconSize = 1.1f;
	private const float DeliveryRowHeight = 1.35f;
	private static readonly Vector3 DeliveryTextOffset = new Vector3(0f, 0.6f, 0f);
	private static readonly Color DeliveryTextColor = new Color(1f, 0.88f, 0.55f);

	// Management research bonus for all planets
	private float researchMultiplier = 1f;

	public MiningController(SignalBus signalBus, PlanetSettings planetSettings, ResourceSettings resourceSettings, IPlayerModel playerModel, ISpaceModel spaceModel, IManagerController managerController, IMothershipController mothershipController, IBoostController boostController)
	{
		this.boostController = boostController;
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
		return researchMultiplier * managerController.GetMiningMultiplier(planetId) * mothershipController.GetMultiplier(RoomType.Engineering) * boostController.MiningMultiplier;
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

	/// <summary>
	/// Loads a ship. When there is more ore than fits, each ore takes its share of the hold,
	/// so a planet's pricier ores don't pile up behind the cheapest one.
	/// </summary>
	public TransferResourcesData[] TransferResources(int planetId, int amount)
	{
		ResourceMiningData[] mined = mineData[planetId].MineDatas;
		float total = 0f;
		foreach (ResourceMiningData resource in mined)
		{
			total += Mathf.Floor(resource.MinedAmount);
		}

		float share = total > amount ? amount / total : 1f;
		List<TransferResourcesData> loaded = new List<TransferResourcesData>();
		int space = amount;
		for (int i = 0; i < mined.Length && space > 0; i++)
		{
			int take = Mathf.Min(space, Mathf.FloorToInt(Mathf.Floor(mined[i].MinedAmount) * share));
			if (take <= 0)
			{
				continue;
			}

			mined[i].MinedAmount -= take;
			space -= take;
			loaded.Add(new TransferResourcesData(mined[i].Type, take));
		}

		// Rounding down can leave a little room, filled from whatever is left
		for (int i = 0; i < mined.Length && space > 0; i++)
		{
			int take = Mathf.Min(space, Mathf.FloorToInt(mined[i].MinedAmount));
			if (take <= 0)
			{
				continue;
			}

			mined[i].MinedAmount -= take;
			space -= take;
			int index = loaded.FindIndex(t => t.Type == mined[i].Type);
			if (index >= 0)
			{
				loaded[index].Amount += take;
			}
			else
			{
				loaded.Add(new TransferResourcesData(mined[i].Type, take));
			}
		}

		return loaded.ToArray();
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

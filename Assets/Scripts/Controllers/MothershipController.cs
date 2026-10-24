using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Prestige: selling the galaxy turns its value into Dark Matter, and mothership rooms bought
/// with Dark Matter give permanent bonuses that carry over into every new galaxy.
/// The restart itself is done by SaveController, which owns what is kept.
/// </summary>
public class MothershipController : IMothershipController, IInitializable, IDisposable
{
	private readonly SignalBus signalBus;
	private readonly IPlayerModel playerModel;
	private readonly ISpaceModel spaceModel;
	private readonly PlanetSettings planetSettings;
	private readonly ISaveService saveService;
	private readonly PrestigeSettings settings;

	private readonly Dictionary<RoomType, int> levels = new Dictionary<RoomType, int>();

	// Running total of money spent on planets and upgrades, saved and reset by a sale
	private long galaxyValue;
	// Saves from before the total was stored work it out once from their planets
	private bool rebuildValue;

	public int GalaxiesSold { get; private set; }

	public MothershipController(SignalBus signalBus, IPlayerModel playerModel, ISpaceModel spaceModel, PlanetSettings planetSettings,
		ISaveService saveService, GameSettings gameSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.spaceModel = spaceModel;
		this.planetSettings = planetSettings;
		this.saveService = saveService;
		settings = gameSettings.PrestigeSettings;
	}

	public void Initialize()
	{
		signalBus.Subscribe<PurchaseSignal>(OnPurchase);
		signalBus.Subscribe<SpaceModelInitializedSignal>(OnSpaceModelInitialized);

		SaveData save = saveService.Load();
		if (save == null)
		{
			return;
		}

		GalaxiesSold = save.GalaxiesSold;
		galaxyValue = save.GalaxyValue;
		rebuildValue = save.GalaxyValue == 0;
		foreach (RoomSaveEntry entry in save.Rooms)
		{
			levels[entry.Type] = entry.Level;
		}
	}

	public void Dispose()
	{
		signalBus.Unsubscribe<PurchaseSignal>(OnPurchase);
		signalBus.Unsubscribe<SpaceModelInitializedSignal>(OnSpaceModelInitialized);
	}

	private void OnPurchase(PurchaseSignal signal)
	{
		if (signal.Success && (signal.Kind == PurchaseKind.Planet || signal.Kind == PurchaseKind.Upgrade))
		{
			galaxyValue += signal.Price;
		}
	}

	private void OnSpaceModelInitialized()
	{
		if (rebuildValue)
		{
			rebuildValue = false;
			galaxyValue = CalculateFromPlanets();
		}
	}

	public List<RoomSaveEntry> GetRoomSaveEntries()
	{
		List<RoomSaveEntry> entries = new List<RoomSaveEntry>();
		foreach (KeyValuePair<RoomType, int> pair in levels)
		{
			entries.Add(new RoomSaveEntry { Type = pair.Key, Level = pair.Value });
		}

		return entries;
	}

	public long GetGalaxyValue()
	{
		return galaxyValue;
	}

	// What the bought planets and their upgrade levels cost, for saves without a stored value
	private long CalculateFromPlanets()
	{
		PlanetData[] planets = spaceModel.GetPlanetsData();
		if (planets == null)
		{
			return 0;
		}

		long value = 0;
		foreach (PlanetData planet in planets)
		{
			PlanetDataSetting setting;
			if (!planet.IsUnlocked || !planetSettings.TryGetPlanetSetting(planet.PlanetIndex, out setting))
			{
				continue;
			}

			value += setting.PlanetPrice;
			value += GetUpgradesValue(setting, planet.MiningRateLevel);
			value += GetUpgradesValue(setting, planet.ShipSpeedLevel);
			value += GetUpgradesValue(setting, planet.ShipCargoLevel);
		}

		return value;
	}

	// Everything paid to bring one stat from level 1 to the given level
	private static long GetUpgradesValue(PlanetDataSetting setting, int level)
	{
		long value = 0;
		for (int l = 1; l < level; l++)
		{
			value += PlanetUpgradeFormula.GetUpgradePrice(setting, l);
		}

		return value;
	}

	public int GetSellReward()
	{
		return Mathf.FloorToInt(settings.RewardScale * Mathf.Sqrt(GetGalaxyValue() / settings.ValueDivider));
	}

	public bool CanSellGalaxy()
	{
		return GetSellReward() >= settings.MinimumReward;
	}

	public void SellGalaxy()
	{
		if (CanSellGalaxy())
		{
			GalaxiesSold++;
			signalBus.Fire(new GalaxySoldSignal() { Reward = GetSellReward() });
		}
	}

	public RoomSetting GetRoom(RoomType type)
	{
		return Array.Find(settings.Rooms, r => r.Type == type);
	}

	public int GetRoomLevel(RoomType type)
	{
		int level;
		return levels.TryGetValue(type, out level) ? level : 0;
	}

	public int GetUpgradeCost(RoomType type)
	{
		RoomSetting room = GetRoom(type);
		int level = GetRoomLevel(type);
		if (room == null || level >= room.MaxLevel)
		{
			return -1;
		}

		return Mathf.RoundToInt(room.BaseCost * Mathf.Pow(room.CostMultiplier, level));
	}

	public bool TryUpgradeRoom(RoomType type)
	{
		int cost = GetUpgradeCost(type);
		bool canBuy = cost >= 0 && playerModel.TryUseCurrency(CurrencyType.DarkMatter, cost);
		if (canBuy)
		{
			levels[type] = GetRoomLevel(type) + 1;
			signalBus.Fire<MothershipUpdatedSignal>();
		}

		signalBus.Fire(new PurchaseSignal() { Kind = PurchaseKind.Room, Success = canBuy, FailMessage = cost >= 0 ? "Not enough Dark Matter" : null });
		return canBuy;
	}

	public float GetMultiplier(RoomType type)
	{
		RoomSetting room = GetRoom(type);
		return room == null ? 1f : 1f + room.BonusPerLevel * GetRoomLevel(type);
	}
}

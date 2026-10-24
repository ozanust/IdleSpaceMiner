using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

/// <summary>
/// Managers like in Idle Planet Miner: hired with Dark Matter, one per planet in a limited number
/// of slots. The primary skill boosts their planet, 3+ star managers also have a secondary skill
/// for everything, and three managers of a rank promote a fourth.
/// </summary>
public class ManagerController : IManagerController, IInitializable, IDisposable
{
	private readonly IPlayerModel playerModel;
	private readonly ISaveService saveService;
	private readonly GameSettings gameSettings;
	private readonly SignalBus signalBus;

	private readonly List<ManagerData> managers = new List<ManagerData>();
	private int nextId;

	public IReadOnlyList<ManagerData> Managers => managers;
	public int Slots { get; private set; }
	public int TotalHired { get; private set; }
	public bool FreeManagerGranted { get; private set; }

	public int AssignedCount
	{
		get
		{
			int count = 0;
			foreach (ManagerData manager in managers)
			{
				if (manager.PlanetId >= 0)
				{
					count++;
				}
			}

			return count;
		}
	}

	public float SmeltSpeedMultiplier => GetGlobalMultiplier(ManagerSecondary.AllSmeltSpeed);
	public float CraftSpeedMultiplier => GetGlobalMultiplier(ManagerSecondary.AllCraftSpeed);

	private ManagerSettings Settings => gameSettings.ManagerSettings;

	public ManagerController(IPlayerModel playerModel, ISaveService saveService, GameSettings gameSettings, SignalBus signalBus)
	{
		this.playerModel = playerModel;
		this.saveService = saveService;
		this.gameSettings = gameSettings;
		this.signalBus = signalBus;
	}

	public void Initialize()
	{
		Slots = Settings.StartingSlots;

		SaveData save = saveService.Load();
		if (save != null && save.Managers != null)
		{
			managers.AddRange(save.Managers);
			Slots = Mathf.Max(save.ManagerSlots, Settings.StartingSlots);
			TotalHired = save.ManagersHired;
			FreeManagerGranted = save.FreeManagerGranted;
			foreach (ManagerData manager in managers)
			{
				nextId = Mathf.Max(nextId, manager.Id + 1);
			}
		}

		signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	public void Dispose()
	{
		signalBus.Unsubscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	// Researching Management comes with a free Public School manager
	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == ResearchType.Management && !FreeManagerGranted)
		{
			FreeManagerGranted = true;
			Add(Roll(GetSchool(ManagerSchool.Public)));
		}
	}

	public ManagerSchoolSetting GetSchool(ManagerSchool school)
	{
		return Array.Find(Settings.Schools, s => s.School == school);
	}

	public bool TryHire(ManagerSchool school, out ManagerData hired)
	{
		hired = null;
		ManagerSchoolSetting setting = GetSchool(school);
		if (setting == null || !playerModel.TryUseCurrency(CurrencyType.DarkMatter, setting.Cost))
		{
			return false;
		}

		hired = Roll(setting);
		Add(hired);
		return true;
	}

	public int GetNextSlotCost()
	{
		int slotNumber = Slots + 1;
		return slotNumber <= 3 ? Settings.ThirdSlotCost : Settings.SlotCostPerSlot * (slotNumber - 1);
	}

	public bool TryBuySlot()
	{
		if (!playerModel.TryUseCurrency(CurrencyType.DarkMatter, GetNextSlotCost()))
		{
			return false;
		}

		Slots++;
		Changed();
		return true;
	}

	public bool TryAssign(int managerId, int planetId)
	{
		ManagerData manager = Find(managerId);
		if (manager == null)
		{
			return false;
		}

		ManagerData current = GetPlanetManager(planetId);
		bool needsSlot = manager.PlanetId < 0 && current == null;
		if (needsSlot && AssignedCount >= Slots)
		{
			return false;
		}

		if (current != null)
		{
			current.PlanetId = -1;
		}

		manager.PlanetId = planetId;
		Changed();
		return true;
	}

	public void Unassign(int managerId)
	{
		ManagerData manager = Find(managerId);
		if (manager != null && manager.PlanetId >= 0)
		{
			manager.PlanetId = -1;
			Changed();
		}
	}

	public ManagerData GetPlanetManager(int planetId)
	{
		return managers.Find(m => m.PlanetId == planetId);
	}

	public bool CanPromote(int managerId)
	{
		ManagerData manager = Find(managerId);
		return manager != null && manager.Stars < Settings.MaxStars && GetPromotionFodder(manager).Count >= Settings.ManagersToPromote;
	}

	/// <summary>Spends three other managers of the same rank (unassigned first) for one more star.</summary>
	public bool TryPromote(int managerId)
	{
		if (!CanPromote(managerId))
		{
			return false;
		}

		ManagerData manager = Find(managerId);
		List<ManagerData> fodder = GetPromotionFodder(manager);
		for (int i = 0; i < Settings.ManagersToPromote; i++)
		{
			managers.Remove(fodder[i]);
		}

		manager.Stars++;
		// The secondary skill is rolled when a manager reaches 3 stars
		if (manager.Secondary == ManagerSecondary.None && manager.Stars >= 3)
		{
			manager.Secondary = RollSecondary();
		}

		Changed();
		return true;
	}

	public float GetMiningMultiplier(int planetId)
	{
		return GetPrimary(planetId, ManagerPrimary.Mining) * GetGlobalMultiplier(ManagerSecondary.AllMining);
	}

	public float GetShipSpeedMultiplier(int planetId)
	{
		return GetPrimary(planetId, ManagerPrimary.ShipSpeed) * GetGlobalMultiplier(ManagerSecondary.AllShipSpeed);
	}

	public float GetCargoMultiplier(int planetId)
	{
		return GetPrimary(planetId, ManagerPrimary.Cargo) * GetGlobalMultiplier(ManagerSecondary.AllCargo);
	}

	public string GetPrimaryText(ManagerData manager)
	{
		return $"{PrimaryName(manager.Primary)} x{GetPrimaryBonus(manager):0.##}";
	}

	public string GetSecondaryText(ManagerData manager)
	{
		if (manager.Secondary == ManagerSecondary.None)
		{
			return manager.Stars < 3 ? "Secondary skill at 3 stars" : "No secondary skill";
		}

		return $"{SecondaryName(manager.Secondary)} x{GetSecondaryBonus(manager):0.##}";
	}

	// --- Bonuses ---

	private float GetPrimary(int planetId, ManagerPrimary type)
	{
		ManagerData manager = GetPlanetManager(planetId);
		return manager != null && manager.Primary == type ? GetPrimaryBonus(manager) : 1f;
	}

	/// <summary>Secondary bonuses of several managers add up, as in Idle Planet Miner.</summary>
	private float GetGlobalMultiplier(ManagerSecondary type)
	{
		float bonus = 1f;
		foreach (ManagerData manager in managers)
		{
			if (manager.PlanetId >= 0 && manager.Secondary == type)
			{
				bonus += GetSecondaryBonus(manager) - 1f;
			}
		}

		return bonus;
	}

	private float GetPrimaryBonus(ManagerData manager)
	{
		float[] table = manager.Primary == ManagerPrimary.Mining ? Settings.MiningBonus : Settings.ShipBonus;
		return table[Mathf.Clamp(manager.Stars, 1, table.Length) - 1];
	}

	private float GetSecondaryBonus(ManagerData manager)
	{
		float[] table;
		switch (manager.Secondary)
		{
			case ManagerSecondary.AllMining:
				table = Settings.AllMiningBonus;
				break;
			case ManagerSecondary.AllShipSpeed:
			case ManagerSecondary.AllCargo:
				table = Settings.AllShipBonus;
				break;
			case ManagerSecondary.AllCraftSpeed:
			case ManagerSecondary.AllSmeltSpeed:
				table = Settings.AllProductionBonus;
				break;
			default:
				return 1f;
		}

		return table[Mathf.Clamp(manager.Stars, 1, table.Length) - 1];
	}

	// --- Helpers ---

	private ManagerData Roll(ManagerSchoolSetting school)
	{
		int stars = school.Stars[school.Stars.Length - 1];
		float roll = Random.value;
		for (int i = 0; i < school.Chances.Length; i++)
		{
			if (roll < school.Chances[i])
			{
				stars = school.Stars[i];
				break;
			}

			roll -= school.Chances[i];
		}

		return new ManagerData
		{
			Id = nextId++,
			Stars = stars,
			Primary = (ManagerPrimary)Random.Range(0, 3),
			Secondary = stars >= 3 ? RollSecondary() : ManagerSecondary.None,
			PlanetId = -1
		};
	}

	private static ManagerSecondary RollSecondary()
	{
		return (ManagerSecondary)Random.Range((int)ManagerSecondary.AllMining, (int)ManagerSecondary.AllSmeltSpeed + 1);
	}

	private void Add(ManagerData manager)
	{
		managers.Add(manager);
		TotalHired++;
		Changed();
	}

	private List<ManagerData> GetPromotionFodder(ManagerData target)
	{
		List<ManagerData> fodder = managers.FindAll(m => m != target && m.Stars == target.Stars);
		// Use managers that aren't working first
		fodder.Sort((a, b) => (a.PlanetId >= 0).CompareTo(b.PlanetId >= 0));
		return fodder;
	}

	private ManagerData Find(int id)
	{
		return managers.Find(m => m.Id == id);
	}

	private void Changed()
	{
		signalBus.Fire<ManagersUpdatedSignal>();
	}

	private static string PrimaryName(ManagerPrimary type)
	{
		switch (type)
		{
			case ManagerPrimary.Mining: return "Mining";
			case ManagerPrimary.ShipSpeed: return "Ship speed";
			default: return "Cargo";
		}
	}

	private static string SecondaryName(ManagerSecondary type)
	{
		switch (type)
		{
			case ManagerSecondary.AllMining: return "All mining";
			case ManagerSecondary.AllShipSpeed: return "All ship speed";
			case ManagerSecondary.AllCargo: return "All cargo";
			case ManagerSecondary.AllCraftSpeed: return "All crafting";
			default: return "All smelting";
		}
	}
}

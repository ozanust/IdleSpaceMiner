using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class PlayerModel : IPlayerModel
{
	private int money;
	private PlanetData[] planetData;
	private Dictionary<ResourceType, int> resources;
	private Dictionary<CurrencyType, int> currencies;
	private List<AlloyType> unlockedAlloys = new List<AlloyType>();
	private List<ResourceType> unlockedItemRecipes = new List<ResourceType>();
	private List<ResearchType> unlockedResearchs = new List<ResearchType>();
	private int lastUnlockedSmelterId = 0;
	private int lastUnlockedCrafterId = 50;
	private int recipeSelectionTargetSmelter = -1;

	readonly SignalBus signalBus;

	public PlayerModel(SignalBus signalBus)
	{
		this.signalBus = signalBus;

		unlockedAlloys.Add(AlloyType.CopperBar);
		unlockedItemRecipes.Add(ResourceType.CopperWire);
	}

	public void AddResource(ResourceType type, int amount)
	{
		if (resources == null)
		{
			resources = new Dictionary<ResourceType, int>();
		}

		if (!resources.ContainsKey(type))
		{
			resources.Add(type, amount);
		}
		else
		{
			resources[type] += amount;
		}

		signalBus.Fire(new PlayerModelUpdatedSignal() { UpdatedResourceType = type });
	}

	public Dictionary<ResourceType, int> GetResources()
	{
		return resources;
	}

	public int GetResource(ResourceType type)
	{
		if (resources == null)
		{
			return 0;
		}

		if (!resources.ContainsKey(type))
		{
			return 0;
		}

		return resources[type];
	}

	public bool HasResource(ResourceType type, int amount)
	{
		if (resources == null)
		{
			return false;
		}

		if (!resources.ContainsKey(type))
		{
			return false;
		}

		return resources[type] >= amount;
	}

	public bool HasResources(ResearchNeededResource[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			if (!HasResource(data[i].Type, data[i].Amount))
			{
				return false;
			}
		}

		return true;
	}

	public bool TryUseResource(ResourceType type, int amount)
	{
		if (resources == null)
		{
			return false;
		}

		if (!resources.ContainsKey(type))
		{
			return false;
		}

		if (resources[type] < amount)
		{
			return false;
		}
		
		resources[type] -= amount;
		signalBus.Fire(new PlayerModelUpdatedSignal() { UpdatedResourceType = type });
		return true;
	}

	public void SellResources(ResourceType type, int amount)
	{
		if (resources == null)
		{
			return;
		}

		if (!resources.ContainsKey(type))
		{
			return;
		}

		if (resources[type] < amount)
		{
			return;
		}

		resources[type] -= amount;
		signalBus.Fire(new PlayerModelUpdatedSignal() { UpdatedResourceType = type });
	}

	public void AddCurrency(CurrencyType type, int amount)
	{
		if (currencies == null)
		{
			currencies = new Dictionary<CurrencyType, int>();
		}

		if (!currencies.ContainsKey(type))
		{
			currencies.Add(type, amount);
		}
		else
		{
			currencies[type] += amount;
		}

		signalBus.Fire<PlayerModelUpdatedSignal>();
	}

	public bool TryUseCurrency(CurrencyType type, int amount)
	{
		if (currencies == null)
		{
			return false;
		}

		if (!currencies.ContainsKey(type))
		{
			return false;
		}

		if (currencies[type] < amount)
		{
			return false;
		}

		currencies[type] -= amount;
		signalBus.Fire<PlayerModelUpdatedSignal>();
		return true;
	}

	public bool HasCurrency(CurrencyType type, int amount)
	{
		if (currencies == null)
		{
			return false;
		}

		if (!currencies.ContainsKey(type))
		{
			return false;
		}

		return currencies[type] >= amount;
	}

	public int GetCurrency(CurrencyType type)
	{
		if (currencies == null)
		{
			return 0;
		}

		if (!currencies.ContainsKey(type))
		{
			return 0;
		}

		return currencies[type];
	}

	public void AddMoney(int amount)
	{
		money += amount;
		signalBus.Fire<PlayerMoneyUpdatedSignal>();
	}

	public void UseMoney(int amount)
	{
		money -= amount;
		signalBus.Fire<PlayerMoneyUpdatedSignal>();
	}

	public bool HasMoney(int amount)
	{
		return money >= amount;
	}

	public int GetMoney()
	{
		return money;
	}

	public void UnlockAlloy(AlloyType type)
	{
		unlockedAlloys.Add(type);
		signalBus.Fire<PlayerModelUpdatedSignal>();
	}

	public AlloyType[] GetUnlockedAlloys()
	{
		return unlockedAlloys.ToArray();
	}

	public void UnlockItemRecipe(ResourceType type)
	{
		unlockedItemRecipes.Add(type);
		signalBus.Fire<PlayerModelUpdatedSignal>();
		signalBus.Fire(new RecipeUnlockedSignal() { Type = type });
	}

	public ResourceType[] GetUnlockedItemRecipes()
	{
		return unlockedItemRecipes.ToArray();
	}

	public void UnlockResearch(ResearchType type)
	{
		unlockedResearchs.Add(type);
		signalBus.Fire<PlayerModelUpdatedSignal>();
		signalBus.Fire(new ResearchCompletedSignal() { ResearchType = type });
	}

	public ResearchType[] GetUnlockedResearchs()
	{
		return unlockedResearchs.ToArray();
	}

	public bool IsResearchUnlocked(ResearchType type)
	{
		return unlockedResearchs.Contains(type);
	}

	public void UnlockSmelter(int smelterId)
	{
		lastUnlockedSmelterId = smelterId;
		signalBus.Fire<PlayerModelUpdatedSignal>();
		signalBus.Fire(new SmelterUnlockedSignal() { SmelterId = lastUnlockedSmelterId });
	}

	public int GetLastUnlockedSmelterId()
	{
		return lastUnlockedSmelterId;
	}

	public void UnlockCrafter(int smelterId)
	{
		lastUnlockedCrafterId = smelterId;
		signalBus.Fire<PlayerModelUpdatedSignal>();
		signalBus.Fire(new SmelterUnlockedSignal() { SmelterId = lastUnlockedCrafterId });
	}

	public int GetLastUnlockedCrafterId()
	{
		return lastUnlockedCrafterId;
	}

	public void SetTargetSmelter(int smelterId)
	{
		recipeSelectionTargetSmelter = smelterId;
	}

	public int GetTargetSmelter()
	{
		return recipeSelectionTargetSmelter;
	}
}

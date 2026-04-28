using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class PlayerModel : IPlayerModel
{
	private int money;
	private PlanetData[] planetData;
	private Dictionary<ResourceType, int> resources;
	private Dictionary<AlloyType, int> alloys;
	private Dictionary<ItemType, int> items;
	private Dictionary<CurrencyType, int> currencies;
	private List<AlloyType> unlockedAlloys = new List<AlloyType>();
	private List<ResourceType> unlockedItemRecipes = new List<ResourceType>();
	private List<ResearchType> unlockedResearchs = new List<ResearchType>();
	private List<int> unlockedSmelters = new List<int>();
	private List<int> unlockedCrafters = new List<int>();
	private Dictionary<int, AlloyType> smeltersWorking = new Dictionary<int, AlloyType>();
	private Dictionary<int, ResourceType> craftersWorking = new Dictionary<int, ResourceType>();
	private int lastUnlockedSmelterId = 0;
	private int lastUnlockedCrafterId = 0;
	private int recipeSelectionTargetSmelter = -1;
	private int recipeSelectionTargetCrafter = -1;

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
	
	public void AddResource(AlloyType type, int amount)
	{
		if (alloys == null)
		{
			alloys = new Dictionary<AlloyType, int>();
		}

		if (!alloys.ContainsKey(type))
		{
			alloys.Add(type, amount);
		}
		else
		{
			alloys[type] += amount;
		}

		signalBus.Fire(new PlayerModelUpdatedSignal() { UpdatedAlloyType = type });
	}
	
	public void AddResource(ItemType type, int amount)
	{
		if (items == null)
		{
			items = new Dictionary<ItemType, int>();
		}

		if (!items.ContainsKey(type))
		{
			items.Add(type, amount);
		}
		else
		{
			items[type] += amount;
		}

		signalBus.Fire(new PlayerModelUpdatedSignal() { UpdatedItemType = type });
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
	
	public bool HasResource(AlloyType type, int amount)
	{
		if (alloys == null)
		{
			return false;
		}

		if (!alloys.ContainsKey(type))
		{
			return false;
		}

		return alloys[type] >= amount;
	}
	
	public bool HasResource(ItemType type, int amount)
	{
		if (items == null)
		{
			return false;
		}

		if (!items.ContainsKey(type))
		{
			return false;
		}

		return items[type] >= amount;
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
	
	public bool TryUseResources(ResearchNeededResource[]  data)
	{
		if (resources == null)
		{
			return false;
		}
		
		for (int i = 0; i < data.Length; i++)
		{
			if (!resources.ContainsKey(data[i].Type))
			{
				return false;
			}
			
			if (resources[data[i].Type] < data[i].Amount)
			{
				return false;
			}
		}
		
		for (int i = 0; i < data.Length; i++)
		{
			resources[data[i].Type] -= data[i].Amount;
			signalBus.Fire(new PlayerModelUpdatedSignal() { UpdatedResourceType = data[i].Type });
		}
		
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
		signalBus.Fire(new RecipeUnlockedSignal() { Type = AlloyToResourceConverter.Convert(type) });
	}

	public AlloyType[] GetUnlockedAlloys()
	{
		return unlockedAlloys.ToArray();
	}

	public int[] GetUnlockedSmelters()
	{
		return unlockedSmelters.ToArray();
	}

	public int[] GetUnlockedCrafters()
	{
		return unlockedCrafters.ToArray();
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

		if (unlockedSmelters != null && !unlockedSmelters.Contains(smelterId))
		{
			unlockedSmelters.Add(smelterId);
		}
		
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
		
		if (unlockedCrafters != null && !unlockedCrafters.Contains(smelterId))
		{
			unlockedCrafters.Add(smelterId);
		}
		
		signalBus.Fire<PlayerModelUpdatedSignal>();
		signalBus.Fire(new CrafterUnlockedSignal() { CrafterId = lastUnlockedCrafterId });
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

	public void SetTargetCrafter(int crafterId)
	{
		recipeSelectionTargetCrafter = crafterId;
	}

	public int GetTargetCrafter()
	{
		return recipeSelectionTargetCrafter;
	}

	public void AddWorkingSmelter(int id, AlloyType type)
	{
		smeltersWorking[id] = type;
	}

	public void AddWorkingCrafter(int id, ResourceType type)
	{
		craftersWorking[id] = type;
	}

	public void RemoveWorkingSmelter(int id)
	{
		smeltersWorking.Remove(id);
	}

	public void RemoveWorkingCrafter(int id)
	{
		craftersWorking.Remove(id);
	}

	public Dictionary<int, AlloyType> GetWorkingSmelters()
	{
		return smeltersWorking;
	}
	
	public Dictionary<int, ResourceType> GetWorkingCrafters()
	{
		return craftersWorking;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using UnityEngine.UI;

public class SmeltRecipesView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] IPlayerModel playerModel;
	[Inject] ResourceSettings resourceSettings;
	[SerializeField] private SmeltRecipeItem smeltRecipeItemPrototype;
	[SerializeField] private GameObject panel;
	[SerializeField] private GameObject smeltRecipeItemsContainer;
	[SerializeField] private GameObject craftRecipeItemsContainer;

	private MenuType type = MenuType.SmeltRecipes;
	private MenuType typeCraft = MenuType.CraftRecipes;
	private List<AlloyType> alloyTypes = new List<AlloyType>();
	private List<ResourceType> craftTypes = new List<ResourceType>();

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);

		Init();
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);
			craftRecipeItemsContainer.gameObject.SetActive(false);
			smeltRecipeItemsContainer.gameObject.SetActive(true);

			/*AlloyType[] types = playerModel.GetUnlockedAlloys();
			foreach (AlloyType type in types)
			{
				if (!alloyTypes.Contains(type))
				{
					SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, smeltRecipeItemsContainer.transform);
					item.SetLockedItemType(AlloyToResourceConverter.Convert(type));
					item.SetSignalBus(signalBus);
					item.SetAlloyType(type);
					item.SetType(SmelterType.Smelter);
					item.SetTitle(type.ToString());
					item.SetDuration(Mathf.RoundToInt(resourceSettings.GetSmeltSetting(type).TimeToSmelt));
					item.OnClick += OnClickRecipe;
					item.Unlock();
					item.Init();
					alloyTypes.Add(type);
				}
			}*/
		}
		else if (signal.Type == typeCraft)
		{
			panel.SetActive(true);
			craftRecipeItemsContainer.gameObject.SetActive(true);
			smeltRecipeItemsContainer.gameObject.SetActive(false);

			/*ResourceType[] types = playerModel.GetUnlockedItemRecipes();
			foreach (ResourceType type in types)
			{
				if (!craftTypes.Contains(type))
				{
					SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, craftRecipeItemsContainer.transform);
					item.SetLockedItemType(type);
					item.SetSignalBus(signalBus);
					item.SetResourceType(type);
					item.SetType(SmelterType.Crafter);
					item.SetTitle(type.ToString());
					item.SetDuration(Mathf.RoundToInt(resourceSettings.GetItemSmeltSetting(type).TimeToSmelt));
					item.OnClickItemRecipe += OnClickItemRecipe;
					item.Unlock();
					item.Init();
					craftTypes.Add(type);
				}
			}*/
		}
	}

	private void Init()
	{
		AlloyType[] types = playerModel.GetUnlockedAlloys();
		foreach (AlloyType type in types)
		{
			if (!alloyTypes.Contains(type))
			{
				SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, smeltRecipeItemsContainer.transform);
				item.SetInjections(signalBus, playerModel, resourceSettings);
				item.SetLockedItemType(AlloyToResourceConverter.Convert(type));
				item.SetAlloyType(type);
				item.SetType(SmelterType.Smelter);
				item.SetTitle(type.ToString());
				item.SetDuration(Mathf.RoundToInt(resourceSettings.GetSmeltSetting(type).TimeToSmelt));
				item.OnClick += OnClickRecipe;
				item.Unlock();
				item.Init();
				alloyTypes.Add(type);
			}
		}

		if (types[types.Length - 1] < AlloyType.AluminumBar)
		{
			AlloyType nextType = types[types.Length - 1] + 1;
			SmeltRecipeItem nextItem = Instantiate(smeltRecipeItemPrototype, smeltRecipeItemsContainer.transform);
			nextItem.SetInjections(signalBus, playerModel, resourceSettings);
			nextItem.SetAlloyType(nextType);
			nextItem.SetType(SmelterType.Smelter);
			nextItem.SetTitle(type.ToString());
			nextItem.SetLockedItemType(AlloyToResourceConverter.Convert(nextType));
			nextItem.OnClick += OnClickRecipe;
			nextItem.Init();
			alloyTypes.Add(nextType);
		}

		ResourceType[] restypes = playerModel.GetUnlockedItemRecipes();
		foreach (ResourceType type in restypes)
		{
			if (!craftTypes.Contains(type))
			{
				SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, craftRecipeItemsContainer.transform);
				item.SetInjections(signalBus, playerModel, resourceSettings);
				item.SetLockedItemType(type);
				item.SetResourceType(type);
				item.SetType(SmelterType.Crafter);
				item.SetTitle(type.ToString());
				item.SetDuration(Mathf.RoundToInt(resourceSettings.GetItemSmeltSetting(type).TimeToSmelt));
				item.OnClickItemRecipe += OnClickItemRecipe;
				item.Unlock();
				item.Init();
				craftTypes.Add(type);
			}
		}

		if (restypes[restypes.Length - 1] < ResourceType.Glass)
		{
			ResourceType nextType = restypes[restypes.Length - 1] + 1;
			SmeltRecipeItem nextCraftItem = Instantiate(smeltRecipeItemPrototype, craftRecipeItemsContainer.transform);
			nextCraftItem.SetInjections(signalBus, playerModel, resourceSettings);
			nextCraftItem.SetResourceType(nextType);
			nextCraftItem.SetType(SmelterType.Crafter);
			nextCraftItem.SetTitle(type.ToString());
			nextCraftItem.SetLockedItemType(nextType);
			nextCraftItem.OnClickItemRecipe += OnClickItemRecipe;
			nextCraftItem.Init();
			craftTypes.Add(nextType);
		}
	}

	private void OnRecipeUnlocked(RecipeUnlockedSignal signal)
	{
		if (signal.Type == ResourceType.Glass)
		{
			return;
		}

		ResourceType nextType = signal.Type + 1;

		if (nextType < ResourceType.CopperWire && nextType > ResourceType.Aluminum)
		{
			AlloyType type = ResourceToAlloyConverter.Convert(nextType);

			if (!alloyTypes.Contains(type))
			{
				SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, smeltRecipeItemsContainer.transform);
				item.SetInjections(signalBus, playerModel, resourceSettings);
				item.SetAlloyType(type);
				item.SetType(SmelterType.Smelter);
				item.SetTitle(type.ToString());
				item.SetLockedItemType(nextType);
				item.OnClick += OnClickRecipe;
				item.Init();
				alloyTypes.Add(type);
			}
		}
		else if (nextType >= ResourceType.CopperWire)
		{
			if (!craftTypes.Contains(nextType))
			{
				SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, craftRecipeItemsContainer.transform);
				item.SetInjections(signalBus, playerModel, resourceSettings);
				item.SetResourceType(nextType);
				item.SetType(SmelterType.Crafter);
				item.SetTitle(type.ToString());
				item.SetLockedItemType(nextType);
				item.OnClickItemRecipe += OnClickItemRecipe;
				item.Init();
				craftTypes.Add(nextType);
			}
		}
	}

	private void OnClickRecipe(AlloyType alloyType)
	{
		signalBus.Fire(new SmeltRecipeAddSignal() { RecipeType = alloyType, SmelterId = playerModel.GetTargetSmelter() });
		panel.SetActive(false);
	}

	private void OnClickItemRecipe(ResourceType resourceType)
	{
		signalBus.Fire(new SmeltRecipeAddSignal() { ItemRecipeType = resourceType, SmelterId = playerModel.GetTargetSmelter() });
		panel.SetActive(false);
	}
}

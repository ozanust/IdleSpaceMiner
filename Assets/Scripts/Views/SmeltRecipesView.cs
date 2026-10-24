using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using UnityEngine.UI;
using TMPro;

public class SmeltRecipesView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] IPlayerModel playerModel;
	[Inject] ResourceSettings resourceSettings;
	[Inject] IProductionController productionController;
	[SerializeField] private SmeltRecipeItem smeltRecipeItemPrototype;
	[SerializeField] private GameObject panel;
	[SerializeField] private GameObject smeltRecipeItemsContainer;
	[SerializeField] private GameObject craftRecipeItemsContainer;
	[Header("Optional")]
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private Button closeButton;

	private MenuType type = MenuType.SmeltRecipes;
	private MenuType typeCraft = MenuType.CraftRecipes;
	private List<AlloyType> alloyTypes = new List<AlloyType>();
	private List<ResourceType> craftTypes = new List<ResourceType>();
	private readonly List<SmeltRecipeItem> items = new List<SmeltRecipeItem>();

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);

		if (closeButton != null)
		{
			closeButton.onClick.AddListener(() => panel.SetActive(false));
		}

		Init();
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			Open(false);
		}
		else if (signal.Type == typeCraft)
		{
			Open(true);
		}
	}

	private void Open(bool crafting)
	{
		panel.SetActive(true);
		craftRecipeItemsContainer.gameObject.SetActive(crafting);
		smeltRecipeItemsContainer.gameObject.SetActive(!crafting);

		if (titleText != null)
		{
			titleText.text = crafting ? "Craft Recipes" : "Smelt Recipes";
		}

		// Research may have changed ingredient amounts and times since the cards were made
		foreach (SmeltRecipeItem item in items)
		{
			item.Refresh();
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
				item.SetInjections(signalBus, playerModel, resourceSettings, productionController);
				items.Add(item);
				item.SetLockedItemType(AlloyToResourceConverter.Convert(type));
				item.SetAlloyType(type);
				item.SetType(SmelterType.Smelter);
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
			nextItem.SetInjections(signalBus, playerModel, resourceSettings, productionController);
				items.Add(nextItem);
			nextItem.SetAlloyType(nextType);
			nextItem.SetType(SmelterType.Smelter);
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
				item.SetInjections(signalBus, playerModel, resourceSettings, productionController);
				items.Add(item);
				item.SetLockedItemType(type);
				item.SetResourceType(type);
				item.SetType(SmelterType.Crafter);
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
			nextCraftItem.SetInjections(signalBus, playerModel, resourceSettings, productionController);
				items.Add(nextCraftItem);
			nextCraftItem.SetResourceType(nextType);
			nextCraftItem.SetType(SmelterType.Crafter);
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
				item.SetInjections(signalBus, playerModel, resourceSettings, productionController);
				items.Add(item);
				item.SetAlloyType(type);
				item.SetType(SmelterType.Smelter);
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
				item.SetInjections(signalBus, playerModel, resourceSettings, productionController);
				items.Add(item);
				item.SetResourceType(nextType);
				item.SetType(SmelterType.Crafter);
				item.SetLockedItemType(nextType);
				item.OnClickItemRecipe += OnClickItemRecipe;
				item.Init();
				craftTypes.Add(nextType);
			}
		}
	}

	private void OnClickRecipe(AlloyType alloyType)
	{
		int targetSmelter = playerModel.GetTargetSmelter();
		signalBus.Fire(new SmeltRecipeAddSignal() { RecipeType = alloyType, SmelterId = targetSmelter });
		panel.SetActive(false);
	}

	private void OnClickItemRecipe(ResourceType resourceType)
	{
		int targetCrafter = playerModel.GetTargetCrafter();
		signalBus.Fire(new CraftRecipeAddSignal() { RecipeType = resourceType, SmelterId = targetCrafter });
		panel.SetActive(false);
	}
}

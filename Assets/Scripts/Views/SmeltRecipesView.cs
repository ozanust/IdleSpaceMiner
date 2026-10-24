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

	/// <summary>
	/// Shows every unlocked recipe plus the next locked one of each kind. The order comes from
	/// the recipe lists in ResourceSettings, so new bars and items only need data.
	/// </summary>
	private void Init()
	{
		foreach (AlloyType alloy in playerModel.GetUnlockedAlloys())
		{
			AddSmeltRecipe(alloy, true);
		}

		foreach (ResourceType item in playerModel.GetUnlockedItemRecipes())
		{
			AddCraftRecipe(item, true);
		}

		AddNextLockedSmeltRecipe();
		AddNextLockedCraftRecipe();
	}

	private void OnRecipeUnlocked(RecipeUnlockedSignal signal)
	{
		// The unlocked card updates itself, the next recipe in line becomes visible
		if (resourceSettings.GetResourceParentType(signal.Type) == MainResourceType.Alloy)
		{
			AddNextLockedSmeltRecipe();
		}
		else
		{
			AddNextLockedCraftRecipe();
		}
	}

	private void AddNextLockedSmeltRecipe()
	{
		foreach (AlloySmeltSettings recipe in resourceSettings.AlloySmeltSettings)
		{
			if (!alloyTypes.Contains(recipe.Type))
			{
				AddSmeltRecipe(recipe.Type, false);
				return;
			}
		}
	}

	private void AddNextLockedCraftRecipe()
	{
		foreach (ItemSmeltSettings recipe in resourceSettings.ItemSmeltTimeSettings)
		{
			if (!craftTypes.Contains(recipe.Type))
			{
				AddCraftRecipe(recipe.Type, false);
				return;
			}
		}
	}

	private void AddSmeltRecipe(AlloyType alloy, bool unlocked)
	{
		if (alloyTypes.Contains(alloy))
		{
			return;
		}

		SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, smeltRecipeItemsContainer.transform);
		item.SetInjections(signalBus, playerModel, resourceSettings, productionController);
		item.SetType(SmelterType.Smelter);
		item.SetLockedItemType(AlloyToResourceConverter.Convert(alloy));
		item.OnClick += OnClickRecipe;
		if (unlocked)
		{
			item.Unlock();
		}

		item.Init();
		items.Add(item);
		alloyTypes.Add(alloy);
	}

	private void AddCraftRecipe(ResourceType resource, bool unlocked)
	{
		if (craftTypes.Contains(resource))
		{
			return;
		}

		SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, craftRecipeItemsContainer.transform);
		item.SetInjections(signalBus, playerModel, resourceSettings, productionController);
		item.SetType(SmelterType.Crafter);
		item.SetLockedItemType(resource);
		item.OnClickItemRecipe += OnClickItemRecipe;
		if (unlocked)
		{
			item.Unlock();
		}

		item.Init();
		items.Add(item);
		craftTypes.Add(resource);
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

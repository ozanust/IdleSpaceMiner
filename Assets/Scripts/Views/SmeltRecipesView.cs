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
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);
			craftRecipeItemsContainer.gameObject.SetActive(false);
			smeltRecipeItemsContainer.gameObject.SetActive(true);

			AlloyType[] types = playerModel.GetUnlockedAlloys();
			foreach (AlloyType type in types)
			{
				if (!alloyTypes.Contains(type))
				{
					SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, smeltRecipeItemsContainer.transform);
					item.SetAlloyType(type);
					item.SetTitle(type.ToString());
					item.SetDuration(Mathf.RoundToInt(resourceSettings.GetSmeltSetting(type).TimeToSmelt));
					item.OnClick += OnClickRecipe;
					alloyTypes.Add(type);
				}
			}
		}
		else if (signal.Type == typeCraft)
		{
			panel.SetActive(true);
			craftRecipeItemsContainer.gameObject.SetActive(true);
			smeltRecipeItemsContainer.gameObject.SetActive(false);

			ResourceType[] types = playerModel.GetUnlockedItemRecipes();
			foreach (ResourceType type in types)
			{
				if (!craftTypes.Contains(type))
				{
					SmeltRecipeItem item = Instantiate(smeltRecipeItemPrototype, craftRecipeItemsContainer.transform);
					item.SetResourceType(type);
					item.SetTitle(type.ToString());
					item.SetDuration(Mathf.RoundToInt(resourceSettings.GetItemSmeltSetting(type).TimeToSmelt));
					item.OnClickItemRecipe += OnClickItemRecipe;
					craftTypes.Add(type);
				}
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

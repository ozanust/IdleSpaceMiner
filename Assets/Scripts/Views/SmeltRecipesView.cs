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

	private MenuType type = MenuType.SmeltRecipes;
	private List<AlloyType> alloyTypes = new List<AlloyType>();

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);

			AlloyType[] types = playerModel.GetUnlockedAlloys();
			foreach(AlloyType type in types)
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
	}

	private void OnClickRecipe(AlloyType alloyType)
	{
		signalBus.Fire(new SmeltRecipeAddSignal() { RecipeType = alloyType, SmelterId = playerModel.GetTargetSmelter() });
		panel.SetActive(false);
	}
}

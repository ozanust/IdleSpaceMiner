using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using UnityEngine.UI;

public class SmeltRecipesView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] PlayerModel playerModel;
	[SerializeField] private SmeltRecipeItem smeltRecipeItemPrototype;
	[SerializeField] private GameObject panel;

	private MenuType type = MenuType.SmeltRecipes;

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
				// create smelt recipe items
			}
		}
	}
}

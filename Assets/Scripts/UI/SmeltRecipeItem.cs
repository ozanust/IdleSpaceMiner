using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Zenject;

public class SmeltRecipeItem : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] IPlayerModel playerModel;
	[Inject] ResourceSettings resourceSettings;
	[SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image iconImage;
	[SerializeField] private bool isUnlock;

	AlloyType alloyType;
	ResourceType resourceType = ResourceType.IronNail;
	public Action<AlloyType> OnClick;

	int price;

	private void Awake()
	{
		button.onClick.AddListener(OnClickButton);
		signalBus.Subscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
	}

	private void Start()
	{
		ResourceType[] unlockedRecipes = playerModel.GetUnlockedItemRecipes();
		SetUnlockItemType(unlockedRecipes[unlockedRecipes.Length - 1] + 1);
	}

	public void SetTitle(string title)
	{
        titleText.text = title;
	}

    public void SetDuration(int smeltDuration)
	{
        durationText.text = smeltDuration.ToString() + "s";
	}

	public void SetAlloyType(AlloyType alloyType)
	{
		this.alloyType = alloyType;
	}

	public void SetUnlockItemType(ResourceType resourceType)
	{
		this.resourceType = resourceType;
		price = resourceSettings.GetItemSmeltSetting(resourceType).PriceToUnlock;
		priceText.text = price.ToString();

		iconImage.sprite = resourceSettings.GetItemData(resourceType).Icon;
		titleText.text = resourceSettings.GetItemData(resourceType).Name;
	}

	private void OnRecipeUnlocked(RecipeUnlockedSignal signal)
	{
		if (isUnlock)
		{
			if (signal.Type > ResourceType.CopperWire && signal.Type > resourceType)
			{
				SetUnlockItemType(signal.Type + 1);
			}
		}
	}

	private void OnClickButton()
	{
		if (!isUnlock)
		{
			OnClick?.Invoke(alloyType);
		}
		else
		{
			if (playerModel.HasMoney(price))
			{
				playerModel.UnlockItemRecipe(resourceType);
			}
		}
	}
}

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
    [SerializeField] private TMP_Text unlockText;
    [SerializeField] private TMP_Text sourceNeededAmountText;
    [SerializeField] private TMP_Text secondSourceNeededAmountText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image sourceImage;
    [SerializeField] private Image secondSourceImage;
    [SerializeField] private Image targetImage;
	[SerializeField] private bool isLocked;
	[SerializeField] private GameObject unlockedRecipeImages;
	[SerializeField] private SmelterType type;

	AlloyType targetAlloyType;
	ResourceType resourceType = ResourceType.IronNail;
	public Action<AlloyType> OnClick;
	public Action<ResourceType> OnClickItemRecipe;

	int price;

	public void Init()
	{
		button.onClick.AddListener(OnClickButton);
		signalBus.Subscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
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
		this.targetAlloyType = alloyType;
	}

	public void SetResourceType(ResourceType resourceType)
	{
		this.resourceType = resourceType;
	}
	public void SetType(SmelterType type)
	{
		this.type = type;
	}

	public void SetInjections(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;
	}

	public void SetLockedItemType(ResourceType targetResourceType)
	{
		this.resourceType = targetResourceType;
		if (targetResourceType <= ResourceType.AluminumBar)
		{
			targetAlloyType = ResourceToAlloyConverter.Convert(targetResourceType);
			AlloySmeltSettings settings = resourceSettings.GetSmeltSetting(targetAlloyType);
			price = settings.PriceToUnlock;
			durationText.text = settings.TimeToSmelt.ToString();
			sourceNeededAmountText.text = settings.ResourceNeeded.ToString();
			sourceImage.sprite = resourceSettings.GetResourceData(AlloyToResourceConverter.ConvertToRaw(targetAlloyType)).Icon;
			targetImage.sprite = resourceSettings.GetAlloyData(targetAlloyType).Icon;
			priceText.text = price.ToString();
			iconImage.sprite = resourceSettings.GetAlloyData(targetAlloyType).Icon;
			titleText.text = resourceSettings.GetAlloyData(targetAlloyType).Name;
		}
		else
		{
			ItemSmeltSettings settings = resourceSettings.GetItemSmeltSetting(targetResourceType);
			price = settings.PriceToUnlock;
			durationText.text = settings.TimeToSmelt.ToString();
			priceText.text = price.ToString();

			ResearchNeededResource[] resources = settings.NeededResources;
			if (resources.Length > 1)
			{
				sourceNeededAmountText.text = resources[0].Amount.ToString();
				sourceImage.sprite = resourceSettings.GetItemData(resources[0].Type).Icon;

				secondSourceNeededAmountText.text = resources[1].Amount.ToString();
				secondSourceImage.sprite = resourceSettings.GetItemData(resources[1].Type).Icon;

				secondSourceImage.gameObject.SetActive(true);
			}
			else
			{
				sourceNeededAmountText.text = resources[0].Amount.ToString();
				sourceImage.sprite = resourceSettings.GetItemData(resources[0].Type).Icon;
			}

			targetImage.sprite = resourceSettings.GetResourceData(resourceType).Icon;
			iconImage.sprite = resourceSettings.GetItemData(targetResourceType).Icon;
			titleText.text = resourceSettings.GetItemData(targetResourceType).Name;
		}

		isLocked = true;
	}

	private void OnRecipeUnlocked(RecipeUnlockedSignal signal)
	{
		if (signal.Type == resourceType)
		{
			Unlock();
		}
		else if (ResourceToAlloyConverter.Convert(signal.Type) == targetAlloyType)
		{
			Unlock();
		}
	}

	public void Unlock()
	{
		priceText.gameObject.SetActive(false);
		iconImage.gameObject.SetActive(false);
		unlockText.gameObject.SetActive(false);
		durationText.gameObject.SetActive(true);
		unlockedRecipeImages.gameObject.SetActive(true);
		isLocked = false;
	}

	private void OnClickButton()
	{
		if (!isLocked)
		{
			OnClick?.Invoke(targetAlloyType);
			OnClickItemRecipe?.Invoke(resourceType);
		}
		else
		{
			if (playerModel.HasMoney(price))
			{
				if (type == SmelterType.Crafter)
				{
					playerModel.UnlockItemRecipe(resourceType);
				}
				else
				{
					playerModel.UnlockAlloy(targetAlloyType);
				}
			}
		}
	}
}

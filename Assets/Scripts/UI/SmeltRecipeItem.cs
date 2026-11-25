using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Zenject;

/// <summary>
/// A recipe card in the recipe picker: ingredients, result and time, plus the unlock price
/// while the recipe is locked. Values include research bonuses.
/// </summary>
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
	[Header("Optional")]
	[SerializeField] private RectTransform arrowImage;
	[SerializeField] private GameObject lockedFooter;
	[SerializeField] private Image thirdSourceImage;
	[SerializeField] private TMP_Text thirdSourceNeededAmountText;

	private IProductionController productionController;
	AlloyType targetAlloyType;
	ResourceType resourceType = ResourceType.IronNail;
	public Action<AlloyType> OnClick;
	public Action<ResourceType> OnClickItemRecipe;

	long price;

	private bool IsAlloy => resourceSettings.GetResourceParentType(resourceType) == MainResourceType.Alloy;

	public void Init()
	{
		button.onClick.AddListener(OnClickButton);
		signalBus.Subscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
	}

	private void OnDestroy()
	{
		signalBus?.TryUnsubscribe<RecipeUnlockedSignal>(OnRecipeUnlocked);
	}

	public void SetTitle(string title)
	{
        titleText.text = title;
	}

    public void SetDuration(float seconds)
	{
        durationText.text = "Time " + NumberFormatter.FormatDuration(seconds);
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

	public void SetInjections(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings, IProductionController productionController = null)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;
		this.productionController = productionController;
	}

	public void SetLockedItemType(ResourceType targetResourceType)
	{
		resourceType = targetResourceType;
		if (IsAlloy)
		{
			targetAlloyType = ResourceToAlloyConverter.Convert(targetResourceType);
			price = resourceSettings.GetSmeltSetting(targetAlloyType).PriceToUnlock;
		}
		else
		{
			price = resourceSettings.GetItemSmeltSetting(targetResourceType).PriceToUnlock;
		}

		titleText.text = GetName(resourceType);
		targetImage.sprite = GetIcon(resourceType);
		priceText.text = "Unlock " + NumberFormatter.FormatMoney(price);

		// Locked recipes still show what they make, dimmed
		unlockedRecipeImages.SetActive(true);
		SetRecipeAlpha(0.55f);
		Refresh();
		isLocked = true;
	}

	/// <summary>Updates ingredient amounts and time, e.g. after research changed them.</summary>
	public void Refresh()
	{
		if (IsAlloy)
		{
			AlloySmeltSettings settings = resourceSettings.GetSmeltSetting(targetAlloyType);
			ResearchNeededResource[] ingredients = productionController != null
				? productionController.GetSmeltIngredients(targetAlloyType)
				: settings.NeededResources != null && settings.NeededResources.Length > 0
					? settings.NeededResources
					: new[] { new ResearchNeededResource { Type = AlloyToResourceConverter.ConvertToRaw(targetAlloyType), Amount = settings.ResourceNeeded } };
			SetIngredients(ingredients);
			SetDuration(productionController != null ? productionController.GetSmeltTime(targetAlloyType) : settings.TimeToSmelt);
		}
		else
		{
			ItemSmeltSettings settings = resourceSettings.GetItemSmeltSetting(resourceType);
			SetIngredients(settings.NeededResources);
			SetDuration(productionController != null ? productionController.GetCraftTime(resourceType) : settings.TimeToSmelt);
		}
	}

	private void SetIngredients(ResearchNeededResource[] ingredients)
	{
		Image[] images = { sourceImage, secondSourceImage, thirdSourceImage };
		TMP_Text[] amounts = { sourceNeededAmountText, secondSourceNeededAmountText, thirdSourceNeededAmountText };
		int count = Mathf.Min(ingredients.Length, thirdSourceImage != null ? 3 : 2);

		for (int i = 0; i < count; i++)
		{
			images[i].sprite = GetIcon(ingredients[i].Type);
			amounts[i].text = NumberFormatter.Format(ingredients[i].Amount);
		}

		RecipeLineLayout.Layout(images, count, arrowImage, targetImage.rectTransform);
	}

	// All ores, bars and items are in the resource data, unlike the per kind lookups
	private Sprite GetIcon(ResourceType type)
	{
		ResourceDataSetting data = resourceSettings.GetResourceData(type);
		return data != null ? data.Icon : null;
	}

	private string GetName(ResourceType type)
	{
		ResourceDataSetting data = resourceSettings.GetResourceData(type);
		return data != null ? data.Name : type.ToString();
	}

	private void SetRecipeAlpha(float alpha)
	{
		CanvasGroup group = unlockedRecipeImages.GetComponent<CanvasGroup>();
		if (group != null)
		{
			group.alpha = alpha;
		}
	}

	private void OnRecipeUnlocked(RecipeUnlockedSignal signal)
	{
		if (signal.Type == resourceType)
		{
			Unlock();
		}
	}

	public void Unlock()
	{
		priceText.gameObject.SetActive(false);
		iconImage.gameObject.SetActive(false);
		unlockText.gameObject.SetActive(false);
		if (lockedFooter != null)
		{
			lockedFooter.SetActive(false);
		}

		durationText.gameObject.SetActive(true);
		unlockedRecipeImages.gameObject.SetActive(true);
		SetRecipeAlpha(1f);
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

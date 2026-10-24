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

	// Icon spacing and size in the recipe line, for one and for two ingredients
	private const float IconSpacing = 115f;
	private const float IconSize = 90f;
	private const float TwoIngredientSpacing = 88f;
	private const float TwoIngredientIconSize = 72f;

	private IProductionController productionController;
	AlloyType targetAlloyType;
	ResourceType resourceType = ResourceType.IronNail;
	public Action<AlloyType> OnClick;
	public Action<ResourceType> OnClickItemRecipe;

	long price;

	private bool IsAlloy => resourceType <= ResourceType.AluminumBar;

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
			int needed = productionController != null ? productionController.GetSmeltResourceNeeded(targetAlloyType) : settings.ResourceNeeded;
			ResourceType ore = AlloyToResourceConverter.ConvertToRaw(targetAlloyType);
			SetIngredients(new[] { new ResearchNeededResource { Type = ore, Amount = needed } });
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
		sourceImage.sprite = GetIcon(ingredients[0].Type);
		sourceNeededAmountText.text = NumberFormatter.Format(ingredients[0].Amount);

		bool hasSecond = ingredients.Length > 1;
		secondSourceImage.gameObject.SetActive(hasSecond);
		if (hasSecond)
		{
			secondSourceImage.sprite = GetIcon(ingredients[1].Type);
			secondSourceNeededAmountText.text = NumberFormatter.Format(ingredients[1].Amount);
		}

		// Ingredients, arrow and result in one centred line, tighter with two ingredients
		float spacing = hasSecond ? TwoIngredientSpacing : IconSpacing;
		float iconSize = hasSecond ? TwoIngredientIconSize : IconSize;
		float[] slots = hasSecond
			? new[] { -1.5f, -0.5f, 0.5f, 1.5f }
			: new[] { -1f, 0f, 1f };
		Place(sourceImage.rectTransform, slots[0], spacing, iconSize);
		if (hasSecond)
		{
			Place(secondSourceImage.rectTransform, slots[1], spacing, iconSize);
		}

		if (arrowImage != null)
		{
			Place(arrowImage, slots[slots.Length - 2], spacing, 0);
		}

		Place(targetImage.rectTransform, slots[slots.Length - 1], spacing, iconSize);
	}

	private static void Place(RectTransform rect, float slot, float spacing, float size)
	{
		rect.anchoredPosition = new Vector2(slot * spacing, rect.anchoredPosition.y);
		if (size > 0)
		{
			rect.sizeDelta = new Vector2(size, size);
		}
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

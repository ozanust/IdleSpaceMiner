using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;

public class CrafterItem : MonoBehaviour
{
    [Inject] SignalBus signalBus;
    [Inject] ResourceSettings resourceSettings;
    [Inject] IProductionController productionController;
    [Inject] IPlayerModel playerModel;

    [SerializeField] private GameObject smelterLockedPanel;
    [SerializeField] private GameObject smelterUnlockedPanel;
    [SerializeField] private TMP_Text smelterUnlockPriceText;

    [SerializeField] private GameObject noRecipeSelectedText;
    [SerializeField] private GameObject selectedRecipeImagesParent;
    [SerializeField] private Image sourceResourceImage;
    [SerializeField] private Image targetResourceImage;
    [SerializeField] private Button setRecipeButton;
    [SerializeField] private Button cancelRecipeButton;
    [SerializeField] private Button unlockButton;
    [SerializeField] private Slider recipeProgressSlider;
    [SerializeField] private TMP_Text recipeRemainingTimeText;

    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private int smelterId;

    [Header("Optional")]
    [SerializeField] private Image secondSourceResourceImage;
    [SerializeField] private RectTransform arrowImage;

    // Icon spacing and size in the recipe line, for one and for two ingredients
    private const float IconSpacing = 105f;
    private const float IconSize = 96f;
    private const float TwoIngredientSpacing = 80f;
    private const float TwoIngredientIconSize = 70f;

    private CrafterAlloyData data;

	private void Awake()
	{
        setRecipeButton.onClick.AddListener(OnClickSetRecipe);
        cancelRecipeButton.onClick.AddListener(OnClickRemoveRecipe);
        unlockButton.onClick.AddListener(OnClickUnlockButton);
    }
	
	[Inject]
	public void Construct(SignalBus sb)
	{
		signalBus = sb;
		signalBus.Subscribe<CrafterUnlockedSignal>(OnCrafterUnlocked);
		signalBus.Subscribe<CraftRecipeAddSignal>(OnCraftRecipeAdded);
		signalBus.Subscribe<CraftRecipeRemoveSignal>(OnCraftRecipeRemoved);
	}

	private void Update()
	{
        if (isUnlocked)
        {
	        if (data != null)
	        {
		        recipeRemainingTimeText.text = data.IsSmelting
			        ? NumberFormatter.FormatDuration(productionController.GetCraftTime(data.Type) * (1f - data.SmeltedTime / data.SmeltTime), true)
			        : "Waiting for materials";
		        recipeProgressSlider.value = data.SmeltedTime;
	        }
        }
	}

    public void SetSmelterUnlockPrice(long price)
    {
        smelterUnlockPriceText.text = NumberFormatter.FormatMoney(price);
    }
    
    private void OnClickUnlockButton()
	{
		productionController.TryUnlockCrafter();
	}

    private void UnlockCrafter(int id)
	{
        smelterUnlockedPanel.SetActive(true);
        smelterLockedPanel.SetActive(false);
        unlockButton.enabled = false;
        isUnlocked = true;
        smelterId = id;
	}

    private void OnCrafterUnlocked(CrafterUnlockedSignal signal)
	{
        if (!isUnlocked)
		{
			UnlockCrafter(signal.CrafterId);
		}
	}

    private void OnClickSetRecipe()
    {
        playerModel.SetTargetCrafter(smelterId);
	    signalBus.Fire(new MenuOpenSignal() { Type = MenuType.CraftRecipes });
    }

    private void OnClickRemoveRecipe()
    {
	    signalBus.Fire(new CraftRecipeRemoveSignal() { SmelterId = smelterId });
    }
    
	private void OnCraftRecipeAdded(CraftRecipeAddSignal signal)
	{
		if (isUnlocked && smelterId == signal.SmelterId && data == null)
		{
			ResearchNeededResource[] ingredients = resourceSettings.GetItemSmeltSetting(signal.RecipeType).NeededResources;

			noRecipeSelectedText.gameObject.SetActive(false);
			selectedRecipeImagesParent.SetActive(true);
			sourceResourceImage.sprite = resourceSettings.GetResourceData(ingredients[0].Type).Icon;
			targetResourceImage.sprite = resourceSettings.GetResourceData(signal.RecipeType).Icon;
			LayoutIngredients(ingredients);
			data = productionController.GetCraftingAlloyData(signal.SmelterId);
			recipeProgressSlider.maxValue = data.SmeltTime;
			recipeProgressSlider.minValue = 0;
			playerModel.AddWorkingCrafter(smelterId, signal.RecipeType);
		}
	}
	
	private void OnCraftRecipeRemoved(CraftRecipeRemoveSignal signal)
	{
		if (signal.SmelterId == smelterId)
		{
			recipeProgressSlider.value = 0;
			recipeRemainingTimeText.text = "Idle";
			noRecipeSelectedText.gameObject.SetActive(true);
			selectedRecipeImagesParent.SetActive(false);
			data = null;
			recipeProgressSlider.maxValue = 0;
			recipeProgressSlider.minValue = 0;
			playerModel.RemoveWorkingCrafter(signal.SmelterId);
		}
	}

    /// <summary>Shows a second ingredient when the item has one, with all icons in one centred line.</summary>
    private void LayoutIngredients(ResearchNeededResource[] ingredients)
    {
        if (secondSourceResourceImage == null || arrowImage == null)
        {
            return;
        }

        bool hasSecond = ingredients.Length > 1;
        secondSourceResourceImage.gameObject.SetActive(hasSecond);
        if (hasSecond)
        {
            secondSourceResourceImage.sprite = resourceSettings.GetResourceData(ingredients[1].Type).Icon;
        }

        // Tighter with two ingredients so the line fits the card
        float spacing = hasSecond ? TwoIngredientSpacing : IconSpacing;
        float iconSize = hasSecond ? TwoIngredientIconSize : IconSize;
        float[] slots = hasSecond ? new[] { -1.5f, -0.5f, 0.5f, 1.5f } : new[] { -1f, 0f, 1f };
        Place(sourceResourceImage.rectTransform, slots[0], spacing, iconSize);
        if (hasSecond)
        {
            Place(secondSourceResourceImage.rectTransform, slots[1], spacing, iconSize);
        }

        Place(arrowImage, slots[slots.Length - 2], spacing, 0);
        Place(targetResourceImage.rectTransform, slots[slots.Length - 1], spacing, iconSize);
    }

    private static void Place(RectTransform rect, float slot, float spacing, float size)
    {
        rect.anchoredPosition = new Vector2(slot * spacing, rect.anchoredPosition.y);
        if (size > 0)
        {
            rect.sizeDelta = new Vector2(size, size);
        }
    }

    public void SetInjections(SignalBus signalBus, ResourceSettings resourceSettings, IProductionController productionController, IPlayerModel playerModel)
	{
        this.signalBus = signalBus;
        this.resourceSettings = resourceSettings;
        this.productionController = productionController;
        this.playerModel = playerModel;
	}
}

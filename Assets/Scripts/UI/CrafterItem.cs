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
    [SerializeField] private Image thirdSourceResourceImage;
    [SerializeField] private RectTransform arrowImage;

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

    /// <summary>Shows up to three ingredients with all icons in one centred line.</summary>
    private void LayoutIngredients(ResearchNeededResource[] ingredients)
    {
        Image[] images = { sourceResourceImage, secondSourceResourceImage, thirdSourceResourceImage };
        int count = 0;
        for (int i = 0; i < ingredients.Length && i < images.Length && images[i] != null; i++)
        {
            images[i].sprite = resourceSettings.GetResourceData(ingredients[i].Type).Icon;
            count++;
        }

        RecipeLineLayout.Layout(images, count, arrowImage, targetResourceImage.rectTransform);
    }

    public void SetInjections(SignalBus signalBus, ResourceSettings resourceSettings, IProductionController productionController, IPlayerModel playerModel)
	{
        this.signalBus = signalBus;
        this.resourceSettings = resourceSettings;
        this.productionController = productionController;
        this.playerModel = playerModel;
	}
}

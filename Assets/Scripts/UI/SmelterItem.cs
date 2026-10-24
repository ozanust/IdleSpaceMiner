using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;

public class SmelterItem : MonoBehaviour
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

    private SmelterAlloyData data;

	private void Awake()
	{
        setRecipeButton.onClick.AddListener(OnClickSetRecipe);
        cancelRecipeButton.onClick.AddListener(OnClickRemoveRecipe);
        unlockButton.onClick.AddListener(OnClickUnlockButton);
    }

	private void Update()
	{
        if (isUnlocked)
        {
	        if (data != null)
	        {
		        recipeRemainingTimeText.text = data.IsSmelting
			        ? NumberFormatter.FormatDuration(productionController.GetSmeltTime(data.Type) * (1f - data.SmeltedTime / data.SmeltTime), true)
			        : "Waiting for ore";
		        recipeProgressSlider.value = data.SmeltedTime;
	        }
        }
	}
	
	[Inject]
	public void Construct(SignalBus sb)
	{
		signalBus = sb;
		signalBus.Subscribe<SmeltRecipeAddSignal>(OnSmeltRecipeAdded);
		signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnSmeltRecipeRemoved);
		signalBus.Subscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
	}

    public void SetSmelterUnlockPrice(long price)
    {
        smelterUnlockPriceText.text = NumberFormatter.FormatMoney(price);
    }
    
    private void OnClickUnlockButton()
	{
		productionController.TryUnlockSmelter();
	}

    private void UnlockSmelter(int id)
	{
        smelterUnlockedPanel.SetActive(true);
        smelterLockedPanel.SetActive(false);
        unlockButton.enabled = false;
        isUnlocked = true;
        smelterId = id;
	}

    private void OnSmelterUnlocked(SmelterUnlockedSignal signal)
	{
        if (!isUnlocked)
		{
			UnlockSmelter(signal.SmelterId);
		}
	}

    private void OnClickSetRecipe()
    {
        playerModel.SetTargetSmelter(smelterId);
	    signalBus.Fire(new MenuOpenSignal() { Type = MenuType.SmeltRecipes });
    }

    private void OnClickRemoveRecipe()
    {
	    signalBus.Fire(new SmeltRecipeRemoveSignal() { SmelterId = smelterId });
    }

    private void OnSmeltRecipeRemoved(SmeltRecipeRemoveSignal signal)
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
            playerModel.RemoveWorkingSmelter(signal.SmelterId);
        }
    }

    private void OnSmeltRecipeAdded(SmeltRecipeAddSignal signal)
	{
        if (isUnlocked && smelterId == signal.SmelterId && data == null)
        {
            noRecipeSelectedText.gameObject.SetActive(false);
            selectedRecipeImagesParent.SetActive(true);
            targetResourceImage.sprite = resourceSettings.GetResourceData(AlloyToResourceConverter.Convert(signal.RecipeType)).Icon;

            // Bars from ore have one ingredient, bars like Bronze and Steel have two
            ResearchNeededResource[] ingredients = productionController.GetSmeltIngredients(signal.RecipeType);
            Image[] images = { sourceResourceImage, secondSourceResourceImage };
            int count = 0;
            for (int i = 0; i < ingredients.Length && i < images.Length && images[i] != null; i++)
            {
                images[i].sprite = resourceSettings.GetResourceData(ingredients[i].Type).Icon;
                count++;
            }

            RecipeLineLayout.Layout(images, count, arrowImage, targetResourceImage.rectTransform);
            data = productionController.GetAlloyData(signal.SmelterId);
            recipeProgressSlider.maxValue = data.SmeltTime;
            recipeProgressSlider.minValue = 0;
            playerModel.AddWorkingSmelter(smelterId, signal.RecipeType);
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

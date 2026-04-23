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
		        recipeRemainingTimeText.text = (data.SmeltTime - data.SmeltedTime).ToString("N1");
		        recipeProgressSlider.value = data.SmeltedTime;
	        }
        }
	}

    public void SetSmelterUnlockPrice(int price)
    {
        smelterUnlockPriceText.text = price.ToString() + "$";
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
		if (isUnlocked && smelterId == signal.SmelterId)
		{
			AlloyDataSetting settings = resourceSettings.GetAlloyData(ResourceToAlloyConverter.Convert(signal.RecipeType));
			ResourceDataSetting sourceSetting = resourceSettings.GetResourceData(signal.RecipeType);

			noRecipeSelectedText.gameObject.SetActive(false);
			selectedRecipeImagesParent.SetActive(true);
			sourceResourceImage.sprite = settings.Icon;
			targetResourceImage.sprite = sourceSetting.Icon;
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
			recipeRemainingTimeText.text = "OFF";
			noRecipeSelectedText.gameObject.SetActive(true);
			selectedRecipeImagesParent.SetActive(false);
			data = null;
			recipeProgressSlider.maxValue = 0;
			recipeProgressSlider.minValue = 0;
			playerModel.RemoveWorkingCrafter(signal.SmelterId);
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

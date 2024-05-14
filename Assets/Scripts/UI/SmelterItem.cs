using System.Collections;
using System.Collections.Generic;
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
    private SmelterAlloyData data;

	private void Awake()
	{
        setRecipeButton.onClick.AddListener(OnClickSetRecipe);
        cancelRecipeButton.onClick.AddListener(OnClickRemoveRecipe);
        unlockButton.onClick.AddListener(OnClickUnlockButton);
    }

	private void Start()
	{
        signalBus.Subscribe<SmeltRecipeAddSignal>(OnSmeltRecipeAdded);
        signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnSmeltRecipeRemoved);
        signalBus.Subscribe<SmelterUnlockedSignal>(OnSmelterUnlocked);
    }

	private void Update()
	{
        if (isUnlocked && data != null)
        {
            recipeRemainingTimeText.text = (data.SmeltTime - data.SmeltedTime).ToString();
            recipeProgressSlider.value = data.SmeltedTime;
        }
	}

    public void SetSmelterUnlockPrice(int price)
    {
        smelterUnlockPriceText.text = price.ToString() + "$";
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
            recipeRemainingTimeText.text = "OFF";
            noRecipeSelectedText.gameObject.SetActive(true);
            selectedRecipeImagesParent.SetActive(false);
            data = null;
            recipeProgressSlider.maxValue = 0;
            recipeProgressSlider.minValue = 0;
        }
    }

    private void OnSmeltRecipeAdded(SmeltRecipeAddSignal signal)
	{
        if (isUnlocked && smelterId == signal.SmelterId)
        {
            AlloyDataSetting settings = resourceSettings.GetAlloyData(signal.RecipeType);
            ResourceDataSetting sourceSetting = resourceSettings.GetResourceData(AlloyToResourceConverter.Convert(signal.RecipeType));

            noRecipeSelectedText.gameObject.SetActive(false);
            selectedRecipeImagesParent.SetActive(true);
            sourceResourceImage.sprite = sourceSetting.Icon;
            targetResourceImage.sprite = settings.Icon;
            data = productionController.GetAlloyData(signal.SmelterId);
            recipeProgressSlider.maxValue = data.SmeltTime;
            recipeProgressSlider.minValue = 0;
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

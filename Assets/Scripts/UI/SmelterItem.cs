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

    [SerializeField] private GameObject smelterLockedPanel;
    [SerializeField] private GameObject smelterUnlockedPanel;
    [SerializeField] private TMP_Text smelterUnlockPriceText;

    [SerializeField] private GameObject noRecipeSelectedText;
    [SerializeField] private GameObject selectedRecipeImagesParent;
    [SerializeField] private Image sourceResourceImage;
    [SerializeField] private Image targetResourceImage;
    [SerializeField] private Button setRecipeButton;
    [SerializeField] private Button cancelRecipeButton;
    [SerializeField] private Slider recipeProgressSlider;
    [SerializeField] private TMP_Text recipeRemainingTimeText;

    private bool isUnlocked = false;
    private int smelterId;
    private SmelterAlloyData data;

	private void Awake()
	{
        setRecipeButton.onClick.AddListener(OnClickSetRecipe);
        cancelRecipeButton.onClick.AddListener(OnClickRemoveRecipe);
        signalBus.Subscribe<SmeltRecipeAddSignal>(OnSmeltRecipeAdded);
        signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnSmeltRecipeRemoved);
    }

	private void Update()
	{
        if (isUnlocked && data != null)
        {
            recipeRemainingTimeText.text = (data.SmeltTime - data.SmeltedTime).ToString();
            recipeProgressSlider.value = data.SmeltedTime;
        }
	}

	public void UnlockSmelter(int id)
	{
        smelterUnlockedPanel.SetActive(true);
        smelterLockedPanel.SetActive(false);
        isUnlocked = true;
        smelterId = id;
	}

    public void SetSmelterUnlockPrice(int price)
	{
        smelterUnlockPriceText.text = price.ToString();
	}

    private void OnClickSetRecipe()
	{
        signalBus.Fire(new MenuOpenSignal() { Type = MenuType.SmeltRecipes });
    }

    private void OnClickRemoveRecipe()
    {
        signalBus.Fire(new SmeltRecipeRemoveSignal() { SmelterId = smelterId });
    }

    private void OnSmeltRecipeRemoved()
	{
        recipeProgressSlider.value = 0;
        recipeRemainingTimeText.text = "OFF";
        noRecipeSelectedText.gameObject.SetActive(true);
        selectedRecipeImagesParent.SetActive(false);
        data = null;
        recipeProgressSlider.maxValue = 0;
        recipeProgressSlider.minValue = 0;
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
}

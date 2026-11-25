using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.UI;
using TMPro;

public class ResourcesSellView : MonoBehaviour
{
    [Inject] ResourceSettings resourceSettings;
    [Inject] SignalBus signalBus;
    [Inject] IPlayerModel playerModel;
    [Inject] IMothershipController mothershipController;

    [SerializeField] private GameObject selectResourceText;
    [SerializeField] private GameObject sellResourcePanel;
    [SerializeField] private Slider sellResourceSlider;
    [SerializeField] private TMP_Text sellingAmountText;
    [SerializeField] private TMP_Text sellingAmountValueText;
    [SerializeField] private Button sellButton;

    int selectedTotalResourceAmount;
    int currentSelectedResourceAmount;
    ResourceType sellingResourceType;


    private void Awake()
	{
        signalBus.Subscribe<ResourceSelectedToSellSignal>(OnResourceSelected);
        signalBus.Subscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
        signalBus.Subscribe<ResourcesViewClosedSignal>(OnResourcesViewClosed);
        signalBus.Subscribe<ResourceDeselectedToSellSignal>(OnResourceDeselectedToSell);
        sellResourceSlider.onValueChanged.AddListener(OnSellingSliderMoved);
        sellButton.onClick.AddListener(OnSellButtonClicked);
        TutorialTarget.Mark(sellButton.gameObject, TutorialTargetId.SellButton);
	}

	private void OnDestroy()
	{
        signalBus.Unsubscribe<ResourceSelectedToSellSignal>(OnResourceSelected);
        signalBus.Unsubscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
        signalBus.Unsubscribe<ResourcesViewClosedSignal>(OnResourcesViewClosed);
        signalBus.Unsubscribe<ResourceDeselectedToSellSignal>(OnResourceDeselectedToSell);
        sellResourceSlider.onValueChanged.RemoveListener(OnSellingSliderMoved);
        sellButton.onClick.RemoveListener(OnSellButtonClicked);
    }

	private void OnResourceSelected(ResourceSelectedToSellSignal signal)
	{
        selectResourceText.SetActive(false);
        sellResourcePanel.SetActive(true);

        selectedTotalResourceAmount = playerModel.GetResource(signal.Type);
        sellingResourceType = signal.Type;

        sellResourceSlider.maxValue = selectedTotalResourceAmount;
        sellResourceSlider.value = selectedTotalResourceAmount;
        sellingAmountText.text = NumberFormatter.Format(selectedTotalResourceAmount);
        sellingAmountValueText.text = NumberFormatter.FormatMoney(GetSellValue(selectedTotalResourceAmount));
    }

    private void OnSellingSliderMoved(float value)
	{
        currentSelectedResourceAmount = Mathf.RoundToInt(value);
        sellingAmountText.text = NumberFormatter.Format(currentSelectedResourceAmount);
        sellingAmountValueText.text = NumberFormatter.FormatMoney(GetSellValue(currentSelectedResourceAmount));
    }

    // Same as the money selling gives, including the Sales room
    private long GetSellValue(int amount)
    {
        return (long)System.Math.Round(amount * resourceSettings.GetResourceValue(sellingResourceType) * (double)mothershipController.GetMultiplier(RoomType.Sales));
    }

    private void OnResourceDeselected()
	{
        selectResourceText.SetActive(true);
        sellResourcePanel.SetActive(false);
    }

    private void OnSellButtonClicked()
	{
        signalBus.Fire(new ResourcesSellSignal() { Type = sellingResourceType, Amount = currentSelectedResourceAmount });
	}

    private void OnPlayerModelUpdated(PlayerModelUpdatedSignal signal)
	{
        if (signal.UpdatedResourceType == sellingResourceType)
        {
            selectedTotalResourceAmount = playerModel.GetResource(sellingResourceType);
            sellResourceSlider.maxValue = selectedTotalResourceAmount;
        }
	}

    private void OnResourcesViewClosed(ResourcesViewClosedSignal signal)
    {
        OnResourceDeselected();
    }

    private void OnResourceDeselectedToSell(ResourceDeselectedToSellSignal signal)
    {
        OnResourceDeselected();
    }
}

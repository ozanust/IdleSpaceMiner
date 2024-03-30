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
	}

	private void OnDestroy()
	{
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
        sellingAmountText.text = selectedTotalResourceAmount.ToString();
        sellingAmountValueText.text = string.Format("{0}{1}", "$", selectedTotalResourceAmount * resourceSettings.GetResourceValue(sellingResourceType));
    }

    private void OnSellingSliderMoved(float value)
	{
        currentSelectedResourceAmount = Mathf.RoundToInt(value);
        sellingAmountText.text = currentSelectedResourceAmount.ToString();
        sellingAmountValueText.text = string.Format("{0}{1}", "$", currentSelectedResourceAmount * resourceSettings.GetResourceValue(sellingResourceType));
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

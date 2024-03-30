using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ResourceInfoItem : MonoBehaviour
{
    [SerializeField] private Button selectionButton;
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TMP_Text resourceNameText;
    [SerializeField] private TMP_Text resourceAmountText;
    [SerializeField] private TMP_Text resourceTotalValueText;

    public void InitializeResource(Sprite icon, string resourceName)
	{
        resourceIcon.sprite = icon;
        resourceIcon.preserveAspect = true;
        resourceNameText.text = resourceName;
        resourceAmountText.text = "0";
        resourceTotalValueText.text = "0";
	}

    public void UpdateAmount(int resourceAmount, int totalValue)
	{
        resourceAmountText.text = resourceAmount.ToString();
        resourceTotalValueText.text = totalValue.ToString();
	}

    public void SetSelectionButtonAction(Action onClick)
	{
        selectionButton.onClick.AddListener(() => onClick?.Invoke());
	}

	private void OnDestroy()
	{
        selectionButton.onClick.RemoveAllListeners();
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchRequirementInfoItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text holdingAmountText;
    [SerializeField] private TMP_Text neededAmountText;

    private int neededAmount;

    // V2
    // implement icon image onClick method to show what the icon means (eg. iron bar)

    public void Initialize(Sprite icon, int holdingAmount, int neededAmount)
    {
        iconImage.sprite = icon;
        holdingAmountText.text = holdingAmount.ToString();
        neededAmountText.text = neededAmount.ToString();
        this.neededAmount = neededAmount;

        if (holdingAmount >= neededAmount)
        {
            holdingAmountText.color = Color.green;
            neededAmountText.color = Color.green;
		}
		else
		{
            holdingAmountText.color = Color.red;
            neededAmountText.color = Color.red;
        }
    }

    public void SetHoldingAmount(int amount)
	{
        holdingAmountText.text = amount.ToString();

        if (amount >= neededAmount)
        {
            holdingAmountText.color = Color.green;
            neededAmountText.color = Color.green;
		}
		else
		{
            holdingAmountText.color = Color.red;
            neededAmountText.color = Color.red;
        }
    }
}
